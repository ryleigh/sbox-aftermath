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

			if( Person.IsDead ) return;

			if ( IsDroppingAmmo )
			{
				_dropAmmoTimer += dt;
				_dropAmmoTotalTime = Math.Min( _dropAmmoTotalTime + dt, DROP_AMMO_TRANSITION_TIME );

				float time = Utils.Map( _dropAmmoTotalTime, 0f, DROP_AMMO_TRANSITION_TIME, DROP_AMMO_TIME_START, DROP_AMMO_TIME_END, EasingType.SineIn );

				if ( _dropAmmoTimer >= time )
				{
					if ( _droppedAmmoType == AmmoType && RemoveSingleAmmo() )
					{
						_ammoDropAmount++;
						_dropAmmoTimer -= time;
					}
					else
					{
						FinishDroppingAmmo();
					}
				}
			}
		}

		public void AddAmmo( AmmoItem ammoItem )
		{
			if ( IsDroppingAmmo )
				FinishDroppingAmmo();

			if ( HasAmmo && AmmoType != ammoItem.AmmoType )
			{
				DropAmmo( AmmoType, AmmoAmount );
				AmmoAmount = 0;
			}

			AmmoType = ammoItem.AmmoType;
			int amountToAdd = Math.Min( ammoItem.AmmoAmount, MaxExtraAmmo - AmmoAmount );

			if ( amountToAdd < ammoItem.AmmoAmount )
			{
				ammoItem.AmmoAmount -= amountToAdd;
				ammoItem.Drop( new Vector2( Rand.Float( -1f, 1f ), Rand.Float( -1f, 1f ) ).Normal, 40f, 5f, 8);
			}
			else
			{
				ammoItem.Delete();
			}

			AmmoAmount += amountToAdd;
		}

		bool DropAmmo( AmmoType ammoType, int ammoAmount )
		{
			if ( ammoType == AmmoType.None || ammoAmount == 0 ) return false;

			AmmoItem ammoItem = new AmmoItem { Position = Person.Position };
			ammoItem.SetPosition2D( Person.Position2D );
			ammoItem.Init( ammoType, ammoAmount );
			ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 100f ), Rand.Float( 3f, 10f ), 8 );

			return true;
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
				AmmoType = AmmoType.None;

			return true;
		}

		public void StartDroppingAmmo()
		{
			if ( IsDroppingAmmo )
				return;

			if ( HasAmmo )
			{
				IsDroppingAmmo = true;
				_ammoDropAmount = 0;
				_dropAmmoTimer = 0f;
				_dropAmmoTotalTime = 0f;
				_droppedAmmoType = AmmoType;
			}
		}

		public void FinishDroppingAmmo( bool fromTool = false )
		{
			if ( !IsDroppingAmmo )
				return;

			IsDroppingAmmo = false;

			if ( _ammoDropAmount == 0 )
			{
				if ( fromTool && AmmoType != AmmoType.None && AmmoAmount > 0 )
				{
					_ammoDropAmount = AmmoAmount;
					_droppedAmmoType = AmmoType;
					AmmoAmount = 0;
					AmmoType = AmmoType.None;
				}
				else
				{
					return;
				}
			}

			DropAmmo( _droppedAmmoType, _ammoDropAmount );
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
