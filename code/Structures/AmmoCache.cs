using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public partial class AmmoCache : Structure
	{
		public AmmoType AmmoType { get; set; }
		public int AmmoAmount { get; set; }
		public bool HasAmmo => AmmoAmount > 0;
		public int MaxAmmoAmount { get; private set; }

		public AmmoCache()
		{
			BlocksMovement = true;
			BlocksSight = true;
			BlocksGunshots = true;

			Height = 72f;

			MaxHp = 100f;
			Hp = MaxHp;
		}

		public override void Spawn()
		{
			SetModel( "models/barrels/square_wooden_box_gold.vmdl" );
			Scale = 1.83f;
			
		}

		public void SetAmmoType( AmmoType ammoType )
		{
			AmmoType = ammoType;

			if ( ammoType == AmmoType.Bullet )
			{
				RenderColor = new Color( 1f, 0.5f, 0.5f );
			} 
			else if ( ammoType == AmmoType.Shell )
			{
				RenderColor = new Color( 1f, 0.6f, 0.3f );
			}
			else if ( ammoType == AmmoType.HPBullet )
			{
				RenderColor = new Color( 0.5f, 0.5f, 1f );
			}
		}
	}
}
