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

		public void EquipGun( Gun gun )
		{
			Gun = gun;
			Gun.PhysicsActive = false;
			Gun.SetParent( Person, true );
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
				return removedGun;
			}

			return null;
		}
	}
}
