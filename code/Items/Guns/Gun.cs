using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox;

namespace aftermath
{
	public enum GunType { None, Pistol, Shotgun, Uzi, Magnum, SawedOffShotgun, BoltActionRifle, AssaultRifle, SniperRifle, Minigun, GrenadeLauncher }

	public partial class Gun : Item
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

		public override void Spawn()
		{
			base.Spawn();
		}

		protected override void Tick()
		{
			float dt = Time.Delta;

			base.Tick();
		}

		public void Drop( Vector2 dir, float force, int numFlips )
		{
			SetPosition2D( new Vector2( Position.x, Position.y ) );
			Velocity2D = dir * force;

			_startingHeight = Position.z;
			_peakHeight = _startingHeight + Rand.Float( 1f, 3f );
			_groundHeight = 1f;
			_airTimeTotal = _peakHeight * 0.01f;
			_airTimer = 0f;

			_startingRotation = Rotation.Yaw();
			_targetRotation = numFlips * 180f;

			IsInAir = true;

			AssignLifetime();
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
	}
}
