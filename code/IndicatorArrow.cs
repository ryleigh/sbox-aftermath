using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class IndicatorArrow : ModelEntity
	{
		public IndicatorArrow()
		{
			Transmit = TransmitType.Never;
		}

		public override void Spawn()
		{
			SetModel( "models/arrow.vmdl" );
			Scale = 1.83f;
		}

		// [Event.Tick.Client]
		// protected virtual void ClientTick()
		// {
		// 	float dt = Time.Delta;
		//
		// 	DebugOverlay.Line( Position.WithZ( 30f ), Position.WithZ( 30f ) + Rotation.Forward * 100f, Color.Black );
		// }
	}
}
