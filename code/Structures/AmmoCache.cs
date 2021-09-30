using System;
using System.Collections.Generic;
using Sandbox;
using System.Linq;

namespace aftermath
{
	public partial class AmmoCache : Structure
	{
		public static Vector3 SlotPos = new Vector3( 0f, 1.5f, 0f ) / 16f;

		public AmmoType AmmoType { get; set; }
		public int AmmoAmount { get; set; }
		public bool HasAmmo => AmmoAmount > 0;
		public int MaxAmmoAmount { get; private set; }

		private readonly float ATTRACT_DIST_SQR = MathF.Pow( 300f, 2f );
		private readonly float ACQUIRE_DIST_SQR = MathF.Pow( 85f, 2f );
		private readonly float ATTRACT_FORCE = 1f;

		public AmmoCache()
		{
			BlocksMovement = true;
			BlocksSight = true;
			BlocksGunshots = true;

			Height = 72f;

			MaxHp = 100f;
			Hp = MaxHp;
		}

		public override void Spawn()
		{
			SetModel( "models/barrels/square_wooden_box_gold.vmdl" );
			Scale = 1.83f;
			
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
				.Where( ammo => ammo.NumPeopleMovingToPickUp == 0)
				.ToList();

			foreach ( var ammo in ammoItems )
			{
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
			// if ( IsBeingBuilt )
			// 	return false;
			//
			// if ( CanGivePersonAmmo( person ) ||
			//    CanReceiveAmmoFromPerson( person ) )
			// 	return true;

			return false;
		}

		// bool CanGivePersonAmmo( Person person )
		// {
		// 	return
		// 		AmmoAmount > 0 &&
		// 		person != null &&
		// 		(AmmoType == person.ExtraAmmoHandler.AmmoType || person.ExtraAmmoHandler.AmmoType == AmmoType.None) &&
		// 		person.ExtraAmmoHandler.AmmoAmount < person.ExtraAmmoHandler.MaxExtraAmmo;
		// }
		//
		// bool CanReceiveAmmoFromPerson( Person person )
		// {
		// 	return
		// 		AmmoAmount < MaxAmmoAmount &&
		// 		person != null &&
		// 		AmmoType == person.ExtraAmmoHandler.AmmoType &&
		// 		person.ExtraAmmoHandler.AmmoAmount > 0;
		// }
		//
		// public override void Interact( Person person )
		// {
		// 	if ( CanGivePersonAmmo( person ) )
		// 	{
		// 		GivePersonAmmo( person );
		// 	}
		// 	else if ( CanReceiveAmmoFromPerson( person ) )
		// 	{
		// 		ReceiveAmmo( person );
		// 	}
		// }
		//
		// void GivePersonAmmo( Person person )
		// {
		// 	int numAmmo = Math.Min( person.ExtraAmmoHandler.MaxExtraAmmo - person.ExtraAmmoHandler.AmmoAmount, AmmoAmount );
		// 	AmmoItem ammoItem = GameMode.ItemManager.CreateAmmoItem( AmmoType, Position + SlotPos, AmmoAmount );
		// 	ammoItem.PersonPickingUp = person;
		// 	PlaceItemCommand placeItemCommand = new PlaceItemCommand( ammoItem, person.Transform.Position, person.Transform.Position.Y + (MathF.Random( 3.5f, 4f ) / 16f), 0, false, false );
		// 	placeItemCommand.StartPlacingItem += OnStartGivingAmmo;
		// 	placeItemCommand.FinishPlacingItem += OnFinishGivingAmmo;
		//
		// 	AmmoAmount -= numAmmo;
		// 	RefreshBlock();
		//
		// 	person.CommandHandler.SetCommand( placeItemCommand );
		// }
	}
}
