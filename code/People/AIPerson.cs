using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Entity = Sandbox.Entity;

namespace aftermath
{
	public partial class AIPerson : Person
	{
		public AIPerson()
		{
			IsAIControlled = true;
		}

		public override void Assign( Player player )
		{
			base.Assign( player );
		}
	}
}
