using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Entity = Sandbox.Entity;

namespace aftermath
{
	public partial class Zombie : AIPerson
	{
		public Zombie()
		{
			PersonType = PersonType.Zombie;
		}

		public override void Assign( Player player )
		{
			base.Assign( player );

			RenderColor = new Color( Rand.Float( 0.2f, 0.25f ), Rand.Float( 0.5f, 0.7f ), Rand.Float( 0.2f, 0.25f ) );
		}
	}
}
