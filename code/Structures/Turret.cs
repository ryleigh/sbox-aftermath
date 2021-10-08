using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox;
using Trace = Sandbox.Trace;

namespace aftermath
{
	public partial class Turret : Structure
	{
		[Net] public Gun Gun { get; private set; } = null;
		public bool HasGun => Gun != null;

		public static Vector3 SlotPos = new Vector3( 0f, 2f, 0f );

		private bool _hasTarget;

		private float _checkRaycastTimer;
		private readonly float CHECK_RAYCAST_DELAY_MIN = 0.2f;
		private readonly float CHECK_RAYCAST_DELAY_MAX = 0.33f;

		private float _shootDelayTimer;

		private bool _isEmpty;

		public Turret()
		{
			BlocksMovement = true;
			BlocksSight = true;
			BlocksGunshots = true;

			Height = 72f;

			MaxHp = 100f;
			Hp = MaxHp;

			StructureType = StructureType.Turret;
			ShowsHoverInfo = true;
			IsUpdateable = true;

			_checkRaycastTimer = Rand.Float( CHECK_RAYCAST_DELAY_MIN, CHECK_RAYCAST_DELAY_MAX );
		}

		public override void Spawn()
		{
			SetModel( "models/citizen_props/crate01.vmdl" );
			Scale = 1.83f;
			RenderColor = new Color( 1f, 1f, 1f );

			base.Spawn();
		}

		public override void Update( float dt )
		{
			base.Update( dt );

			DebugOverlay.Text( Position + new Vector3( 0f, 0f, 0f ), HasGun ? $"{Gun.AmmoAmount}/{Gun.MaxAmmoAmount}" : $"EMPTY", Color.White, 0f, float.MaxValue );

			if ( Gun != null )
			{
				_checkRaycastTimer -= dt;
				if ( _checkRaycastTimer <= 0f )
				{
					_hasTarget = CheckRaycast();
					_checkRaycastTimer = Rand.Float( CHECK_RAYCAST_DELAY_MIN, CHECK_RAYCAST_DELAY_MAX );
				}

				if ( _hasTarget )
				{
					_shootDelayTimer -= dt;
					if ( _shootDelayTimer <= 0f )
					{
						if ( !Gun.Shoot() )
						{
							if ( !_isEmpty )
							{
								BecomeEmpty();
								_isEmpty = true;
							}
						}
						_shootDelayTimer = Rand.Float( Gun.ShootDelayMin, Gun.ShootDelayMax );
					}
				}
			}
		}

		bool CheckRaycast()
		{
			if ( Owner != null && Owner.Survivors.Count > 0)
			{
				// var trace = Trace.Ray( Gun.BarrelPos, Gun.Rotation.Forward * 400f ).EntitiesOnly().WithTag( "person" ).Run();
				var trace = Trace.Ray( Position.WithZ( 50f ), Position.WithZ( 50f ) + Rotation.Forward * 400f ).EntitiesOnly().WithTag( "person" ).Run();
				if ( trace.Hit )
				{
					if ( trace.Entity is Person p )
					{
						var targets = Owner.Survivors[0].GetValidTargets();
						if ( targets.Contains( p ) )
						{
							float dist = (Position2D - p.Position2D).Length;
							AftermathGame.Instance.SpawnFloater( Position, $"{p.PersonName}", new Color( 0f, 0f, 0.8f, 0.5f ) );

							DebugOverlay.Line( Position.WithZ( 50f ), Position.WithZ( 50f ) + Rotation.Forward * dist, Color.Blue, 0.1f );


							// make sure they aren't obscured by a wall
							if ( !AftermathGame.Instance.GridManager.Raycast( Position.WithZ( 50f ), Position.WithZ( 50f ) + Rotation.Forward * dist, RaycastMode.Sight, out _, out _, out _, ignoreStartGridPos: true) )
							{
								AftermathGame.Instance.SpawnFloater( Position, $"{p.PersonName}", new Color( 1f, 0f, 0.8f, 1f ) );
								return true;
							}
						}
							

						// didHitPerson = true;
						// hitPerson = p;
						// hitPosPerson = trace.EndPos;
						// hitNormalPerson = trace.Normal;
					}
				}

				// DebugOverlay.Line( Position.WithZ( 50f ), Position.WithZ( 50f ) + Rotation.Forward * 400f, Color.Blue );
			}

			

			// var ray = new Ray( Gun.BarrelPos, Gun.Transform.Forward );
			//
			// Person hitPerson = null;
			// foreach ( Person person in GameMode.PersonManager.TurretTargets )
			// {
			// 	if ( person == null || person.IsSpawning || person.IsDead )
			// 		continue;
			//
			// 	// check if heading towards person
			// 	float dot = Gun.Transform.Forward.Dot( person.Position2D - Position2D );
			// 	if ( dot < 0f )
			// 		continue;
			//
			// 	EntityRaycastHit hit;
			// 	if ( person.AABBPhysics.Raycast( ray, MathF.Random( Gun.MinRange, Gun.MaxRange ), out hit ) )
			// 	{
			// 		hitPerson = person;
			// 		break;
			// 	}
			// }
			//
			// if ( hitPerson != null )
			// {
			// 	GridPosition gridPos;
			// 	Vector2D hitPos;
			// 	Vector2D normal;
			// 	if ( !GameMode.GridManager.Raycast( Utils.GetVector2D( Gun.BarrelPos ), hitPerson.Position2D, RaycastMode.Sight, out gridPos, out hitPos, out normal ) )
			// 	{
			// 		return true;
			// 	}
			// }

			return false;
		}

		public override void Destroy( Vector2 direction )
		{
			if(IsDestroyed)
				return;

			base.Destroy( direction );

			if ( HasGun )
			{
				Gun.Unequip();
				Gun.Position = Position + SlotPos;
				Gun.Drop(
					new Vector2( Rand.Float( -1f, 1f ), Rand.Float( -1f, 1f ) ).Normal,
					Rand.Float( Person_GunHandler.TOSS_FORCE_MIN, Person_GunHandler.TOSS_FORCE_MAX ),
					Rand.Float( 90f, 120f),
					Rand.Int( Person_GunHandler.TOSS_NUM_FLIPS_MIN, Person_GunHandler.TOSS_NUM_FLIPS_MAX )
				);
			}
		}

		public override bool GetIsInteractable( Person person )
		{
			if ( IsBeingBuilt )
				return false;

			if ( CanPersonAttachGun( person ) ||
			     CanPersonReloadTurret( person ) )
				return true;

			return false;
		}

		bool CanPersonAttachGun( Person person )
		{
			return
				!HasGun &&
				person != null &&
				person.EquippedGun != null;
		}

		bool CanPersonReloadTurret( Person person )
		{
			return
				HasGun &&
				!Gun.HasFullAmmo &&
				person != null &&
				person.AmmoType == Gun.AmmoType &&
				person.AmmoAmount > 0;
		}

		public override void Interact( Person person )
		{
			if ( CanPersonAttachGun( person ) )
			{
				ReceiveGun( person );
			}
			else if ( CanPersonReloadTurret( person ) )
			{
				ReceiveAmmo( person );
			}
		}

		void ReceiveGun( Person person )
		{
			Gun gun = person.GunHandler.UnequipGun();
			if ( gun != null )
			{
				Vector3 targetPos = GetGunSlotPos( gun );

				PlaceItemCommand placeItemCommand = new PlaceItemCommand( gun, targetPos, targetPos.z + (Rand.Float( 8f, 14f )), Rand.Int( 0, 5 ) );
				placeItemCommand.StartPlacingItem += OnStartReceivingGun;
				placeItemCommand.FinishPlacingItem += OnFinishReceivingGun;

				person.CommandHandler.SetCommand( placeItemCommand );
			}
		}

		Vector3 GetGunSlotPos( Gun gun )
		{
			return Position
			    + SlotPos;
		}

		void OnStartReceivingGun( Person person, Item item )
		{
			Gun gun = (Gun)item;
			gun.PersonHolding = null;
			gun.StructureHolding = this;
			gun.PersonPickingUp = null;
		}

		void OnFinishReceivingGun( Person person, Item item )
		{
			Gun gun = (Gun)item;

			Gun = gun;
			gun.MovementActive = false;
			PutGunInSlot( gun );
		}

		public void PutGunInSlot( Gun gun )
		{
			gun.Rotation = Rotation.Identity;
			gun.Rotation.RotateAroundAxis( Vector3.OneZ, Utils.GetDegreesForDirection( FacingDirection ) );

			gun.Position = GetGunSlotPos( gun );
		}

		void ReceiveAmmo( Person person )
		{
			int numAmmo = Math.Min( Gun.MaxAmmoAmount - Gun.AmmoAmount, person.AmmoHandler.AmmoAmount );
			if ( person.AmmoHandler.DropAmmo( Gun.AmmoType, numAmmo, out var ammoItem ) )
			{
				// person.AmmoHandler.AmmoAmount -= numAmmo;
				// if ( person.AmmoHandler.AmmoAmount == 0 )
				// 	person.AmmoHandler.AmmoType = AmmoType.None;

				Vector3 targetPos = GetGunSlotPos( Gun );

				PlaceItemCommand placeItemCommand = new PlaceItemCommand( ammoItem, targetPos, targetPos.z + (Rand.Float( 8f, 14f )), Rand.Int( 0, 5 ) );
				placeItemCommand.StartPlacingItem += OnStartReceivingAmmo;
				placeItemCommand.FinishPlacingItem += OnFinishReceivingAmmo;

				person.CommandHandler.SetCommand( placeItemCommand );
			}
		}

		void OnStartReceivingAmmo( Person person, Item item )
		{
			AmmoItem ammoItem = (AmmoItem)item;

		}

		void OnFinishReceivingAmmo( Person person, Item item )
		{
			AmmoItem ammoItem = (AmmoItem)item;

			if ( IsDestroyed )
			{
				ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 400f ), Rand.Float( 3f, 100f ), 8 );
				return;
			}

			int amountToAdd = Math.Min( ammoItem.AmmoAmount, Gun.MaxAmmoAmount - Gun.AmmoAmount );
			if ( amountToAdd < ammoItem.AmmoAmount )
			{
				ammoItem.AmmoAmount = ammoItem.AmmoAmount - amountToAdd;
				ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 400f ), Rand.Float( 3f, 100f ), 8 );
			}
			else
			{
				ammoItem.Delete();
			}

			Gun.AmmoAmount += amountToAdd;

			if ( _isEmpty )
			{
				BecomeNonEmpty();
				_isEmpty = false;
			}
		}

		public override string GetHoverInfo()
		{
			if ( IsDestroyed )
				return "";

			return HasGun
				? Gun.GunName + "\n" + Gun.AmmoAmount + "/" + Gun.MaxAmmoAmount
				: "Empty Turret";
		}

		void BecomeEmpty()
		{
			AftermathGame.Instance.SpawnFloater( Position, $"OUT OF AMMO!", new Color( 1f, 0f, 0.5f, 1f ) );
		}

		void BecomeNonEmpty()
		{
			
		}
	}
}
