using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox;

namespace aftermath
{
	public enum GunType { None, Pistol, Shotgun, Uzi, Magnum, SawedOffShotgun, BoltActionRifle, AssaultRifle, SniperRifle, Minigun, GrenadeLauncher }

	public abstract partial class Gun : Item
	{
		public Vector3 BarrelPos
		{
			get
			{
				var muzzle = GetAttachment( "muzzle" );
				if ( muzzle.HasValue )
					return muzzle.Value.Position;
				else
					return Position;
			}
		}

		public GunType GunType { get; protected set; }
		public string GunName { get; protected set; }

		public bool IsHeld => PersonHolding != null || StructureHolding != null;
		public Person PersonHolding { get; set; }
		public Structure StructureHolding { get; set; }

		public float MinShotSpeed { get; protected set; }
		public float MaxShotSpeed { get; protected set; }
		public float MinShotLength { get; protected set; }
		public float MaxShotLength { get; protected set; }
		public int MinDamage { get; protected set; }
		public int MaxDamage { get; protected set; }
		public float PenetrationChance { get; protected set; }
		public int NumProjectilesMin { get; protected set; }
		public int NumProjectilesMax { get; protected set; }
		public float SpreadX { get; protected set; }
		public float SpreadY { get; protected set; }
		public float ShootDelayMin { get; protected set; }
		public float ShootDelayMax { get; protected set; }
		public float ShootTimeMin { get; protected set; }
		public float ShootTimeMax { get; protected set; }
		public float RequiredAimQuality { get; protected set; }
		public float AimSpeed { get; protected set; }
		public float ShootForceMin { get; protected set; }
		public float ShootForceMax { get; protected set; }
		public float BulletForceMin { get; protected set; }
		public float BulletForceMax { get; protected set; }
		public float MinRange { get; protected set; }
		public float MaxRange { get; protected set; }

		public float MovementSpeedModifier { get; protected set; }

		public override void Spawn()
		{
			base.Spawn();
		}

		protected override void Tick()
		{
			float dt = Time.Delta;

			base.Tick();
		}

		public void Unequip()
		{
			PersonHolding = null;
			StructureHolding = null;
			PersonPickingUp = null;
			StructurePickingUp = null;
			SetParent( null );
			PhysicsActive = true;
		}

		public bool Shoot()
		{
			Log.Info( $"Shoot!!!! ------------------------------------------------------ {PersonHolding}" );
			// DebugOverlay.Line( BarrelPos, Rotation.Forward * 1000f, Color.Magenta, 3f );
			DebugOverlay.Line( BarrelPos, BarrelPos + (Vector3)( PersonHolding?.Aiming.SightDirection ?? Rotation.Forward) * 250f, Color.Yellow, 0.1f );

			return true;
		}

		public override void PersonStartedPickingUp( Person person )
		{
			base.PersonStartedPickingUp( person );
			person.GunHandler.StartEquippingGun( this );
		}

		public override void PersonFinishedPickingUp( Person person )
		{
			base.PersonFinishedPickingUp( person );
			person.GunHandler.FinishEquippingGun( this );
		}

		public override void PersonInterruptedPickingUp( Person person )
		{
			base.PersonInterruptedPickingUp( person );
			person.GunHandler.InterruptEquippingGun( this );
		}

		public override string GetHoverInfo()
		{
			return GunName;
		}
	}
}
