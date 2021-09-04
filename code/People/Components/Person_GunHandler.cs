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

		public override void Update( float dt )
		{
			if ( Person.IsDead ) return;

		}

		public void StartEquippingGun( Gun gun )
		{
			if(Gun != null)
				DropGun( Vector2.Right, 40f, 8 );

			Gun = gun;
			Person.EquippedGun = gun;

			gun.PersonHolding = Person;
			gun.PhysicsActive = false;
			gun.SetIsHovered( false );
		}

		public void FinishEquippingGun( Gun gun )
		{
			gun.SetParent( Person, true );
		}

		public void InterruptEquippingGun( Gun gun )
		{
			gun.PersonHolding = null;
			gun.PhysicsActive = true;
		}

		public void DropGun( Vector2 dir, float force, int numFlips )
		{
			if ( Gun == null ) return;

			Gun gun = UnequipGun();
			gun?.Drop( dir, force, numFlips );
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
	}
}
