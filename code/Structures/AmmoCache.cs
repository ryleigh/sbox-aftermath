using System;
using System.Collections.Generic;
using Sandbox;
using System.Linq;

namespace aftermath
{
	public partial class AmmoCache : Structure
	{
		public static Vector3 SlotPos = new Vector3( 0f, 1.5f, 0f ) / 16f;

		[Net] public AmmoType AmmoType { get; set; }
		[Net] public int AmmoAmount { get; set; }
		public bool HasAmmo => AmmoAmount > 0;
		[Net] public int MaxAmmoAmount { get; private set; }

		private readonly float ATTRACT_DIST_SQR = MathF.Pow( 220f, 2f );
		private readonly float ACQUIRE_DIST_SQR = MathF.Pow( 60f, 2f );
		private readonly float ATTRACT_FORCE = 0.5f;

		public AmmoCache()
		{
			BlocksMovement = true;
			BlocksSight = true;
			BlocksGunshots = true;

			Height = 72f;

			MaxHp = 100f;
			Hp = MaxHp;

			ShowsHoverInfo = true;
			IsUpdateable = true;

			MaxAmmoAmount = 100;
		}

		public override void Spawn()
		{
			SetModel( "models/barrels/square_wooden_box_gold.vmdl" );
			Scale = 1.83f;
			
			base.Spawn();
		}

		public void SetAmmoType( AmmoType ammoType )
		{
			AmmoType = ammoType;

			if ( ammoType == AmmoType.Bullet )
			{
				RenderColor = new Color( 1f, 0.5f, 0.5f );
				StructureType = StructureType.AmmoCacheBullets;
			}
			else if ( ammoType == AmmoType.Shell )
			{
				RenderColor = new Color( 1f, 0.6f, 0.3f );
				StructureType = StructureType.AmmoCacheShells;
			}
			else if ( ammoType == AmmoType.HPBullet )
			{
				RenderColor = new Color( 0.5f, 0.5f, 1f );
				StructureType = StructureType.AmmoCacheHPBullets;
			}
		}

		public override void Update( float dt )
		{
			base.Update( dt );

			DebugOverlay.Text( Position, 0, $"{AmmoType}\n{AmmoAmount}/{MaxAmmoAmount}", GetFloaterColorForAmmo(), 0f, float.MaxValue );

			if ( !IsBeingBuilt && !IsDestroyed && AmmoAmount < MaxAmmoAmount )
			{
				HandleAmmoAttraction( dt );
			}
		}

		void HandleAmmoAttraction( float dt )
		{
			List<AmmoItem> ammoItems = Entity.All.OfType<AmmoItem>()
				.Where( ammo => ammo.AmmoType == AmmoType)
				.Where( ammo => !ammo.IsInAir )
				.Where( ammo => !ammo.IsBeingHovered )
				.Where( ammo => !ammo.IsBeingPickedUp)
				.Where( ammo => ammo.CarryingPerson == null)
				.Where( ammo => ammo.NumPeopleMovingToPickUp == 0)
				.ToList();

			foreach ( var ammo in ammoItems )
			{
				if ( !ammo.MovementActive )
					ammo.MovementActive = true;

				float distSqr = (ammo.Position2D - Position2D).LengthSquared;

				if ( distSqr < ACQUIRE_DIST_SQR )
				{
					Vector3 targetPos = Position + SlotPos;
					ammo.PlaceItem( targetPos, Rand.Float( 90f, 140f ), 0.3f, 0 );
					ammo.StructurePickingUp = this;
					ammo.HitGroundCallback += CollectAmmo;
				}
				else if ( distSqr < ATTRACT_DIST_SQR )
				{
					float force = Utils.Map( distSqr, ATTRACT_DIST_SQR, 0f, 0f, ATTRACT_FORCE, EasingType.ExpoIn );
					ammo.Velocity2D += (Position2D - ammo.Position2D) * force;
				}
			}
		}

		void CollectAmmo( Item item )
		{
			AmmoItem ammoItem = (AmmoItem)item;

			int amountToAdd = Math.Min( ammoItem.AmmoAmount, MaxAmmoAmount - AmmoAmount );

			if ( amountToAdd < ammoItem.AmmoAmount )
			{
				GridPosition adjacentGridPos = AftermathGame.Instance.GridManager.GetAdjacentEmptyGridPositionDiagonal( GridPosition );
				if ( adjacentGridPos.IsValid )
				{
					ammoItem.Position = Position + SlotPos;

					Vector2 targetPos = AftermathGame.Instance.GridManager.Get2DPosForGridPos( adjacentGridPos ) + new Vector2( Rand.Float( -0.3f, 0.3f ), Rand.Float( -0.3f, 0.3f ) ) * AftermathGame.Instance.GridManager.SquareSize;
					ammoItem.PlaceItem( targetPos, Rand.Float( 70f, 100f ), Rand.Float( 0.9f, 1.4f ), 0);

					ammoItem.StructurePickingUp = null;
					ammoItem.HitGroundCallback -= CollectAmmo;
				}
				else
				{
					ammoItem.Delete();
				}
			}
			else
			{
				ammoItem.Delete();
			}

			AmmoAmount += amountToAdd;

			AftermathGame.Instance.SpawnFloater( Position, $"+{amountToAdd} Ammo", Color.White );
		}

		public override bool GetIsInteractable( Person person )
		{
			if ( IsBeingBuilt )
				return false;

			return CanGivePersonAmmo( person ) || CanReceiveAmmoFromPerson( person );
		}

		bool CanGivePersonAmmo( Person person )
		{
			return
				AmmoAmount > 0 &&
				person != null &&
				(AmmoType == person.AmmoType || person.AmmoType == AmmoType.None) &&
				person.AmmoAmount < person.MaxAmmoAmount;
		}

		bool CanReceiveAmmoFromPerson( Person person )
		{
			return
				AmmoAmount < MaxAmmoAmount &&
				person != null &&
				AmmoType == person.AmmoType &&
				person.AmmoAmount > 0;
		}

		public override void Interact( Person person )
		{
			if ( CanGivePersonAmmo( person ) )
				GivePersonAmmo( person );
			else if ( CanReceiveAmmoFromPerson( person ) )
				ReceiveAmmo( person );
		}

		void GivePersonAmmo( Person person )
		{
			int numAmmo = Math.Min( person.AmmoHandler.MaxExtraAmmo - person.AmmoHandler.AmmoAmount, AmmoAmount );
			AmmoItem ammoItem = AftermathGame.Instance.CreateAmmoItem( Position + SlotPos, AmmoType, AmmoAmount );

			ammoItem.PersonPickingUp = person;
			PlaceItemCommand placeItemCommand = new PlaceItemCommand( ammoItem, person.Transform.Position, Rand.Float( 90f, 130f ), 0, false, false );
			placeItemCommand.StartPlacingItem += OnStartGivingAmmo;
			placeItemCommand.FinishPlacingItem += OnFinishGivingAmmo;

			AmmoAmount -= numAmmo;

			person.CommandHandler.SetCommand( placeItemCommand );
		}

		void OnStartGivingAmmo( Person person, Item item )
		{
			AmmoItem ammoItem = (AmmoItem)item;
		}

		void OnFinishGivingAmmo( Person person, Item item )
		{
			AmmoItem ammoItem = (AmmoItem)item;
			if ( person == null || person.IsDead )
			{
				ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 75f ), Rand.Float( 5f, 12f ), 0 );
				ammoItem.PersonPickingUp = null;
				return;
			}

			person.AmmoHandler.AddAmmo( ammoItem );

			AftermathGame.Instance.SpawnFloater( person.Position2D, $"+{ammoItem.AmmoAmount}", GetFloaterColorForAmmo() );
		}

		void ReceiveAmmo( Person person )
		{
			int numAmmo = Math.Min( MaxAmmoAmount - AmmoAmount, person.AmmoHandler.AmmoAmount );

			if ( person.AmmoHandler.DropAmmo( AmmoType, numAmmo, out var ammoItem ) )
			{
				// person.AmmoHandler.AmmoAmount -= numAmmo;
				//
				// if ( person.AmmoHandler.AmmoAmount == 0 )
				// 	person.AmmoHandler.AmmoType = AmmoType.None;

				ammoItem.StructurePickingUp = this;

				Vector3 targetPos = Position + SlotPos;
				PlaceItemCommand placeItemCommand = new PlaceItemCommand( ammoItem, targetPos, Rand.Float( 90f, 130f ), 0 );
				placeItemCommand.StartPlacingItem += OnStartReceivingAmmo;
				placeItemCommand.FinishPlacingItem += OnFinishReceivingAmmo;

				person.CommandHandler.SetCommand( placeItemCommand );
			}
		}

		void OnStartReceivingAmmo( Person person, Item item )
		{
			AmmoItem ammoItem = (AmmoItem)item;

			AftermathGame.Instance.SpawnFloater( person.Position2D, $"-{ammoItem.AmmoAmount}", GetFloaterColorForAmmo() );
		}

		void OnFinishReceivingAmmo( Person person, Item item )
		{
			AmmoItem ammoItem = (AmmoItem)item;

			if ( IsDestroyed )
			{
				ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 75f ), Rand.Float( 5f, 12f ), 0 );
				return;
			}

			int amountToAdd = Math.Min( ammoItem.AmmoAmount, MaxAmmoAmount - AmmoAmount );
			AmmoAmount += amountToAdd;

			ammoItem.Delete();
		}

		public override string GetHoverInfo()
		{
			if ( IsDestroyed )
				return "";

			string name = "";
			if ( AmmoType == AmmoType.Bullet ) name = "Bullets";
			else if ( AmmoType == AmmoType.Shell ) name = "Shells";
			else if ( AmmoType == AmmoType.HPBullet ) name = "High-Powered Bullets";

			return name + "\n" + AmmoAmount + "/" + MaxAmmoAmount;
		}

		Color GetFloaterColorForAmmo()
		{
			if ( AmmoType == AmmoType.Bullet )
				return new Color( 0.6f, 0.6f, 0.1f );
			else if ( AmmoType == AmmoType.Shell )
				return new Color( 0.8f, 0.5f, 0.2f );
			else if ( AmmoType == AmmoType.HPBullet )
				return new Color( 0.2f, 0.2f, 0.7f );

			return Color.White;
		}

		public override void Destroy( Vector2 direction )
		{
			if ( IsDestroyed )
				return;

			base.Destroy( direction );

			if ( HasAmmo )
			{
				AmmoItem ammoItem = AftermathGame.Instance.CreateAmmoItem( Position + SlotPos, AmmoType, AmmoAmount );
				ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 75f ), Rand.Float( 5f, 12f ), 0 );
			}
		}
	}
}
