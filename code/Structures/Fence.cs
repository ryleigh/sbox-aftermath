using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public partial class Fence : Structure
	{
		public Fence()
		{
			BlocksMovement = true;
			BlocksSight = false;
			BlocksGunshots = false;

			Height = 72f;

			MaxHp = 35f;
			Hp = MaxHp;
		}

		public override void Spawn()
		{
			SetModel( "models/square_wooden_box.vmdl" );
			Scale = 1.83f;
			RenderColor = new Color( 1f, 1f, 1f, 0.5f );
		}
	}
}
