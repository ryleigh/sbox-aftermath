using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class BuildingIndicator : ModelEntity
	{
		public BuildingIndicator()
		{
			Transmit = TransmitType.Never;
		}

		public override void Spawn()
		{
			SetModel( "models/square_wooden_box.vmdl" );
			Scale = 1.83f;
		}
	}
}
