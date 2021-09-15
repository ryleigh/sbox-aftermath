using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public partial class Pistol : Gun
	{
		public Pistol()
		{
			GunName = "Pistol";
			GunType = GunType.Pistol;

			MinShotSpeed = 1800f;
			MaxShotSpeed = 2000f;
			MinShotLength = 90f;
			MaxShotLength = 110f;
			MinDamage = 9;
			MaxDamage = 13;
			PenetrationChance = 0f;
			NumProjectilesMin = 1;
			NumProjectilesMax = 1;
			SpreadX = 10f;
			SpreadY = 4f;
			ShootDelayMin = 0.25f;
			ShootDelayMax = 0.5f;
			ShootTimeMin = 0.15f;
			ShootTimeMax = 0.5f;
			RequiredAimQuality = 0.95f;
			AimSpeed = 6f;
			ShootForceMin = 0.15f;
			ShootForceMax = 0.36f;
			BulletForceMin = 0.55f;
			BulletForceMax = 0.75f;
			MinRange = 280f;
			MaxRange = 330f;

			AmmoType = AmmoType.Bullet;
			MaxAmmoAmount = 16;
			AmmoAmount = Rand.Int( 5, 12 );
			ReloadTimePerAmmo = 0.33f;

			MovementSpeedModifier = 0.95f;
		}

		public override void Spawn()
		{
			ModelPath = "weapons/rust_pistol/rust_pistol.vmdl";

			base.Spawn();
		}
	}
}
