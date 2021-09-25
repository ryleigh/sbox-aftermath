using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public partial class Shotgun : Gun
	{
		public Shotgun()
		{
			GunName = "Shotgun";
			GunType = GunType.Shotgun;

			MinShotSpeed = 1450;
			MaxShotSpeed = 1550f;
			MinShotLength = 90f;
			MaxShotLength = 110f;
			MinDamage = 7;
			MaxDamage = 11;
			PenetrationChance = 0f;
			NumProjectilesMin = 7;
			NumProjectilesMax = 10;
			SpreadX = 25f;
			SpreadY = 4f;
			ShootDelayMin = 0.6f;
			ShootDelayMax = 1.8f;
			ShootTimeMin = 0.2f;
			ShootTimeMax = 0.8f;
			RequiredAimQuality = 0.8f;
			AimSpeed = 4f;
			ShootForceMin = 0.3f;
			ShootForceMax = 1.1f;
			BulletForceMin = 0.3f;
			BulletForceMax = 0.5f;
			MinRange = 240f;
			MaxRange = 280;

			AmmoType = AmmoType.Shell;
			MaxAmmoAmount = 8;
			AmmoAmount = Rand.Int( 3, MaxAmmoAmount );
			ReloadTimePerAmmo = 0.45f;

			MovementSpeedModifier = 0.75f;
		}

		public override void Spawn()
		{
			ModelPath = "weapons/rust_pumpshotgun/rust_pumpshotgun.vmdl";

			base.Spawn();
		}
	}
}
