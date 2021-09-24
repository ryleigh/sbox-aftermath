using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		[Net] public Person PersonHolding { get; set; }
		[Net] public Structure StructureHolding { get; set; }

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

		public AmmoType AmmoType { get; set; }
		[Net] public int AmmoAmount { get; set; }
		public int MaxAmmoAmount { get; protected set; }
		public bool HasAmmo => AmmoAmount > 0;
		public bool HasFullAmmo => AmmoAmount == MaxAmmoAmount;
		public float ReloadTimePerAmmo { get; protected set; }
		[Net] public float ReloadTimer { get; set; }
		public bool IsReloading => ReloadTimer > 0f;

		private int _currentNumExtraShots;
		public int NumExtraShotsMin { get; protected set; }
		public int NumExtraShotsMax { get; protected set; }
		private float _extraShotDelay;
		public float MinExtraShotDelay { get; protected set; }
		public float MaxExtraShotDelay { get; protected set; }

		public float MovementSpeedModifier { get; protected set; }

		public override void Spawn()
		{
			base.Spawn();

		}

		protected override void Tick()
		{
			float dt = Time.Delta;

			DebugText = AmmoAmount > 0 ? $"{AmmoAmount}/{MaxAmmoAmount} {Person_AmmoHandler.GetDisplayName( AmmoType, AmmoAmount != 1 )}" : "";
			DebugText += $"\nIsHeld: {IsHeld}, IsServer: {IsServer}, ammo: {AmmoAmount}/{MaxAmmoAmount}";

			if ( IsReloading && PersonHolding != null )
				HandleReloading( dt );

			HandleExtraShots( dt );

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
			// this.DrawText( $"AmmoAmount: {AmmoAmount}", 8, 0.5f, 0.1f );
			if ( AmmoAmount <= 0 )
			{
				AftermathGame.Instance.SpawnFloater( Position, $"{GunName} OUT OF AMMO!", new Color( 1f, 0.5f, 0.5f ) );
				return true;
			}

			// DebugOverlay.Line( BarrelPos, Rotation.Forward * 1000f, Color.Magenta, 3f );
			// DebugOverlay.Line( BarrelPos, BarrelPos + dir * 250f, Color.Yellow, 0.1f );

			AmmoAmount--;
			GenerateExtraShots();

			ShootProjectiles();

			ApplyShootForce();

			return true;
		}

		protected virtual void ShootProjectiles()
		{
			AftermathGame.Instance.SpawnFloater( Position, $"{GunName} SHOOT!", new Color( 1f, 1f, 0.4f, 0.2f ) );

			Vector3 dir = (Vector3)(PersonHolding?.Aiming.BodyDirection ?? Rotation.Forward);
			Rotation rot = global::Rotation.From( new Angles( dir.x, dir.y, dir.z ) );
			Vector3 from = BarrelPos + dir * 20f;

			for ( int i = 0; i < Rand.Int( NumProjectilesMin, NumProjectilesMax ); i++ )
			{
				float spreadFactor = PersonHolding?.GunSpreadFactor ?? 1f;
				Vector3 shootDir = new Vector3( rot.Pitch() + Utils.Deg2Rad( Rand.Float( -SpreadX, SpreadX ) * spreadFactor ), rot.Yaw(), rot.Roll() );

				Gunshot shot = new Gunshot();
				shot.Init( 
					pos: from, 
					dir: shootDir, 
					length: Rand.Float( MinShotLength, MaxShotLength), 
					speed: Rand.Float( MinShotSpeed, MaxShotSpeed), 
					damage: Rand.Float( MinDamage, MaxDamage ),
					bulletForce: Rand.Float( BulletForceMin, BulletForceMax ),
					penetrationChance: PenetrationChance, 
					PersonHolding, 
					StructureHolding, 
					AmmoType 
				);
			}
		}

		protected virtual void ApplyShootForce()
		{
			Vector3 dir = (Vector3)(PersonHolding?.Aiming.BodyDirection ?? Rotation.Forward);
			PersonHolding?.Movement.AddForceVelocity( -Utils.GetVector2( dir ) * Rand.Float( ShootForceMin, ShootForceMax ) * PersonHolding.GunShootForceFactor );
		}

		private void GenerateExtraShots()
		{
			if ( _currentNumExtraShots == 0 && AmmoAmount > 0 )
			{
				_currentNumExtraShots = Rand.Int( NumExtraShotsMin, NumExtraShotsMax );
				_extraShotDelay = Rand.Float( MinExtraShotDelay, MaxExtraShotDelay );
			}
		}

		private void HandleExtraShots(float dt)
		{
			if ( _currentNumExtraShots > 0 )
			{
				_extraShotDelay -= dt;
				if ( _extraShotDelay <= 0f )
				{
					Shoot();

					if ( AmmoAmount <= 0 )
					{
						_currentNumExtraShots = 0;
					}
					else
					{
						_currentNumExtraShots--;
						_extraShotDelay = Rand.Float( MinExtraShotDelay, MaxExtraShotDelay );
					}
				}
			}
		}

		private void HandleReloading( float dt )
		{
			ReloadTimer -= dt * PersonHolding.ReloadSpeedFactor;
			if ( ReloadTimer <= 0f )
			{
				ReloadSingleAmmo();
			}
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

		public void ReloadSingleAmmo()
		{
			if( AmmoAmount == MaxAmmoAmount ) return;

			AmmoAmount = Math.Min( AmmoAmount + 1, MaxAmmoAmount );
		}

		public void StartReloading()
		{
			// if already reloading, restart the anim but add the ammo
			if ( IsReloading )
			{
				ReloadSingleAmmo();
			}

			ReloadTimer = ReloadTimePerAmmo;
		}

		public void StopReloading()
		{
			if ( IsReloading )
			{
				ReloadSingleAmmo();
			}

			ReloadTimer = 0f;
		}

		public override string GetHoverInfo()
		{
			return GunName + $" isheld: {IsHeld}, isserver: {IsServer}, ammo: {AmmoAmount}/{MaxAmmoAmount}";
		}
	}
}
