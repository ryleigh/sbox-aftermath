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

		public Gun()
		{
			GunName = "Gun";
			
		}

		public override void Spawn()
		{
			ModelPath = "weapons/rust_pistol/rust_pistol.vmdl";

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
