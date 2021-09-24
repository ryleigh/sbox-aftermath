using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public partial class Factory : Structure
	{
		public Factory()
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
			SetModel( "models/square_wooden_box.vmdl" );
			Scale = 1.83f;
			RenderColor = new Color( 0f, 0f, 0f );
		}
	}
}
