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

		private AmmoItem _ammoItem;

		public static string GetDisplayName( AmmoType ammoType, bool plural )
		{
			string displayName = "";

			if ( ammoType == AmmoType.Bullet )
				return plural ? "Bullets" : "Bullet";
			else if ( ammoType == AmmoType.Shell )
				return plural ? "Shells" : "Shell";
			else if ( ammoType == AmmoType.HPBullet )
				return plural ? "High-Powered Bullets" : "High-Powered Bullet";
			else if ( ammoType == AmmoType.Grenade )
				return plural ? "Grenades" : "Grenade";

			return displayName;
		}

		public Person_AmmoHandler()
		{

		}

		public override void Update( float dt )
		{
			base.Update( dt );

			if ( Person.IsDead ) return;
		}

		public void AddAmmo( AmmoItem ammoItem )
		{
			// if we're carrying a diff type of ammo, drop it
			if ( HasAmmo && AmmoType != ammoItem.AmmoType )
				DropAllAmmo();

			if ( !HasAmmo )
			{
				AmmoType = ammoItem.AmmoType;
				int amountToAdd = Math.Min( ammoItem.AmmoAmount, MaxExtraAmmo - AmmoAmount );
				int amountToDrop = ammoItem.AmmoAmount - amountToAdd;

				_ammoItem = ammoItem;
				_ammoItem.SetCarryingPerson( Person );

				if ( amountToDrop > 0 )
					CreateAmmoItem( AmmoType, amountToDrop );

				AmmoAmount += amountToAdd;
			}
			else
			{
				int amountToAdd = Math.Min( ammoItem.AmmoAmount, MaxExtraAmmo - AmmoAmount );
				int amountToDrop = ammoItem.AmmoAmount - amountToAdd;

				if ( amountToDrop > 0 )
				{
					ammoItem.SetAmmoAmount( amountToDrop );
					ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 100f ), Rand.Float( 3f, 10f ), 8 );
				}
				else
				{
					ammoItem.Delete();
				}

				AmmoAmount += amountToAdd;
			}

			_ammoItem?.SetAmmoAmount( AmmoAmount );
		}

		void CreateAmmoItem( AmmoType ammoType, int ammoAmount )
		{
			AmmoItem droppedAmmoItem = new AmmoItem { Position = Person.Position };
			droppedAmmoItem.SetPosition2D( Person.Position2D );
			droppedAmmoItem.Init( ammoType, ammoAmount );
			droppedAmmoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 100f ), Rand.Float( 3f, 10f ), 8 );
		}

		bool DropAmmo( AmmoType ammoType, int ammoAmount )
		{
			if ( ammoType == AmmoType.None || ammoAmount == 0 ) return false;

			if ( ammoAmount == AmmoAmount && _ammoItem != null )
			{
				_ammoItem.RemoveCarryingPerson();
				_ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 100f ), Rand.Float( 3f, 10f ), 8 );
				_ammoItem = null;
			}
			else
			{
				AmmoItem ammoItem = new AmmoItem { Position = Person.Position };
				ammoItem.SetPosition2D( Person.Position2D );
				ammoItem.Init( ammoType, ammoAmount );
				ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 100f ), Rand.Float( 3f, 10f ), 8 );
			}

			return true;
		}

		public bool DropAmmo( AmmoType ammoType, int ammoAmount, out AmmoItem ammoItem )
		{
			if ( ammoType == AmmoType.None || ammoAmount == 0 )
			{
				ammoItem = null;
				return false;
			}

			if ( ammoAmount == AmmoAmount && _ammoItem != null )
			{
				ammoItem = _ammoItem;
				ammoItem.RemoveCarryingPerson();
				ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 100f ), Rand.Float( 3f, 10f ), 8 );
				_ammoItem = null;
				return true;
			}
			else
			{
				ammoItem = new AmmoItem { Position = Person.Position };
				ammoItem.SetPosition2D( Person.Position2D );
				ammoItem.Init( ammoType, ammoAmount );
				ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 100f ), Rand.Float( 3f, 10f ), 8 );
				return true;
			}
		}

		public bool RemoveSingleAmmo()
		{
			if ( AmmoType == AmmoType.None )
				return false;

			if ( AmmoAmount == 0 )
			{
				AmmoType = AmmoType.None;
				return false;
			}

			AmmoAmount--;
			if ( AmmoAmount == 0 )
			{
				AmmoType = AmmoType.None;

				_ammoItem?.Delete();
				_ammoItem = null;
			}
			else
			{
				_ammoItem.SetAmmoAmount( AmmoAmount );
			}

			return true;
		}

		public void DropAllAmmo()
		{
			if ( HasAmmo )
			{
				if ( IsDroppingAmmo )
				{
					if ( AmmoType == _droppedAmmoType )
					{
						DropAmmo( AmmoType, AmmoAmount + _ammoDropAmount );
					}
					else
					{
						DropAmmo( AmmoType, AmmoAmount );
						DropAmmo( _droppedAmmoType, _ammoDropAmount );
					}
				}
				else
				{
					DropAmmo( AmmoType, AmmoAmount );
				}
			}
			else
			{
				if ( IsDroppingAmmo )
				{
					DropAmmo( _droppedAmmoType, _ammoDropAmount );
				}
			}

			AmmoType = AmmoType.None;
			AmmoAmount = 0;
			IsDroppingAmmo = false;
		}
	}
}
