using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Entity = Sandbox.Entity;

namespace aftermath
{
	public partial class Survivor : Person
	{
		public Survivor()
		{
			PersonType = PersonType.Survivor;
		}

		public override void Assign( Player player )
		{
			base.Assign( player );
			RenderColor = player.TeamColor;
		}
	}
}
