using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public partial class Turret : Structure
	{
		public Turret()
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
			SetModel( "models/citizen_props/crate01.vmdl" );
			Scale = 1.83f;
			RenderColor = new Color( 1f, 1f, 1f );
		}
	}
}
