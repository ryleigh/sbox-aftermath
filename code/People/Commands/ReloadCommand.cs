using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class ReloadCommand : PersonCommand
	{
		private float _reloadingTimer;

		public override string ToString() { return $"Reload: {_reloadingTimer}"; }

		public ReloadCommand()
		{
			Type = PersonCommandType.Reload;
		}

		public override void Begin()
		{
			base.Begin();

			if ( !Person.GunHandler.HasGun || Person.GunHandler.Gun.AmmoAmount == Person.GunHandler.Gun.MaxAmmoAmount || !Person.AmmoHandler.HasAmmo ||
				Person.GunHandler.Gun.AmmoType != Person.AmmoHandler.AmmoType )
			{
				Finish();
				return;
			}
		}

		public override void Update( float dt )
		{
			if ( IsFinished )
				return;

			if ( Person == null || Person.IsDead || !Person.GunHandler.HasGun )
			{
				Finish();
				return;
			}

			Gun gun = Person.GunHandler.Gun;

			if ( gun.AmmoAmount == Person.GunHandler.Gun.MaxAmmoAmount || !Person.AmmoHandler.HasAmmo )
			{
				Finish();
				return;
			}

			_reloadingTimer += dt * Person.ReloadSpeedFactor;

			if ( _reloadingTimer >= Person.GunHandler.Gun.ReloadTimePerAmmo )
			{
				if ( Person.AmmoHandler.RemoveSingleAmmo() )
				{
					gun.StartReloading();
					// Person.Scaler.Scale( 0.9f, 0.25f );
				}

				_reloadingTimer -= gun.ReloadTimePerAmmo;
			}
		}

		public override void Interrupt()
		{
			base.Interrupt();

			if ( Person != null && Person.GunHandler.HasGun )
				Person.GunHandler.Gun.StopReloading();
		}
	}
}
