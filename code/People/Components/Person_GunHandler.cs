using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox;

namespace aftermath
{
	public partial class Person_GunHandler : PersonComponent
	{
		public Gun Gun { get; private set; }
		public bool HasGun => Gun != null;

		public const float DROP_FORCE_MIN = 1.2f;
		public const float DROP_FORCE_MAX = 1.6f;
		public const int DROP_NUM_FLIPS_MIN = 1;
		public const int DROP_NUM_FLIPS_MAX = 3;

		public const float TOSS_FORCE_MIN = 1.5f;
		public const float TOSS_FORCE_MAX = 2f;
		public const int TOSS_NUM_FLIPS_MIN = 1;
		public const int TOSS_NUM_FLIPS_MAX = 3;

		public override void Update( float dt )
		{
			if ( Person.IsDead ) return;

		}

		public void StartEquippingGun( Gun gun )
		{
			if(Gun != null)
				DropGun( Vector2.Right, 40f, Rand.Float( 1f, 3f ), 8 );

			Gun = gun;
			Person.EquippedGun = gun;

			gun.PersonHolding = Person;
			gun.MovementActive = false;
			gun.SetIsHovered( false );
		}

		public void FinishEquippingGun( Gun gun )
		{
			gun.SetParent( Person, true );
		}

		public void InterruptEquippingGun( Gun gun )
		{
			// gun.PersonHolding = null;
			// gun.PhysicsActive = true;

			gun.Unequip();
			gun.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 75f ), Rand.Float( 3f, 8f ), 8 );
		}

		public void DropGun( Vector2 dir, float force, float peakHeightOffset, int numFlips )
		{
			if ( Gun == null ) return;

			Gun gun = UnequipGun();
			gun?.Drop( dir, force, peakHeightOffset, numFlips );
		} 

		public Gun UnequipGun()
		{
			if ( Gun != null )
			{
				Gun removedGun = Gun;
				Gun.Unequip();
				Gun = null;
				Person.EquippedGun = null;

				return removedGun;
			}

			return null;
		}

		public void OutOfAmmo()
		{
			if ( !Reload() )
			{
				if ( Person.PersonType == PersonType.Survivor )
				{
					AftermathGame.Instance.SpawnFloater( Person.Position, $"{Person.GunHandler.Gun?.GunName ?? "NULL"} OUT OF AMMO!", new Color( 1f, 0.5f, 0.5f ) );

					// sfx

					Person.CommandHandler.SetCommand( new WaitCommand( Rand.Float( 3f, 6f ) ) );
				}
				else
				{
					DropGun(
						new Vector2( Rand.Float( -1f, 1f ), Rand.Float( -1f, 1f ) ).Normal,
						Rand.Float( DROP_FORCE_MIN, DROP_FORCE_MAX ),
						3f,
						Rand.Int( DROP_NUM_FLIPS_MIN, DROP_NUM_FLIPS_MAX )
					);
				}
			}
		}

		public bool Reload()
		{
			if ( HasGun && Person.AmmoHandler.HasAmmo && Gun.AmmoType == Person.AmmoHandler.AmmoType )
			{
				AftermathGame.Instance.SpawnFloater( Person.Position, $"RELOADING!", new Color( 0.5f, 0.5f, 1f ) );

				Person.CommandHandler.SetCommand( new ReloadCommand() );
				return true;
			}

			return false;
		}
	}
}
