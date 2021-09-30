using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public partial class Wall : Structure
	{
		public Wall( )
		{
			BlocksMovement = true;
			BlocksSight = true;
			BlocksGunshots = true;

			Height = 72f;

			MaxHp = 50f;
			Hp = MaxHp;

			StructureType = StructureType.Wall;
		}

		public override void Spawn()
		{
			SetModel( "models/square_wooden_box.vmdl" );
			Scale = 1.83f;
		}
	}
}
