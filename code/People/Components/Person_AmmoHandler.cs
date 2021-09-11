using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public enum AmmoType { None, Bullet, Shell, HPBullet, Grenade };

	public class Person_AmmoHandler : PersonComponent
	{
		public AmmoType AmmoType { get; set; }
		public int AmmoAmount { get; set; }
		public bool HasAmmo => AmmoType != AmmoType.None;
		public int MaxExtraAmmo { get; private set; } = 50;

		public bool IsDroppingAmmo;
		private float _dropAmmoTimer;
		private float _dropAmmoTotalTime;
		private const float DROP_AMMO_TIME_START = 0.33f;
		private const float DROP_AMMO_TIME_END = 0.04f;
		private const float DROP_AMMO_TRANSITION_TIME = 0.5f;

		private int _ammoDropAmount;
		private AmmoType _droppedAmmoType;

		public static string GetDisplayName( AmmoType ammoType )
		{
			string displayName = "";

			if ( ammoType == AmmoType.Bullet )
				return "Bullets";
			else if ( ammoType == AmmoType.Shell )
				return "Shells";
			else if ( ammoType == AmmoType.HPBullet )
				return "High-Powered Bullets";
			else if ( ammoType == AmmoType.Grenade )
				return "Grenades";

			return displayName;
		}

		public Person_AmmoHandler()
		{

		}

		public override void Update( float dt )
		{
			base.Update( dt );

			if( Person.IsDead ) return;
			

		}
	}
}
