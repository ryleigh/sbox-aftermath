using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	internal partial class RoundGlobals : Globals
	{
		[Net, OnChangedCallback] public BaseRound Round { get; set; }
		public BaseRound LastRound { get; private set; }

		private void OnRoundChanged()
		{
			if ( LastRound != Round )
			{
				LastRound?.Finish();
				LastRound = Round;
				LastRound.Start();
			}
		}

		[Event.Tick]
		private void Tick()
		{
			Round?.OnTick();
		}
	}

	public static partial class Rounds
	{
		private static Globals<RoundGlobals> Variables => Globals.Define<RoundGlobals>( "rounds" );
		public static BaseRound Current => Variables.Value?.Round;

		public static void Change( BaseRound round )
		{
			Log.Info( $"Rounds.Change: {round}, Variables.Value: {Variables.Value}" );
			Assert.NotNull( round );

			var entity = Variables.Value;

			if ( entity.IsValid() )
			{
				entity.Round?.Finish();
				entity.Round = round;
				entity.Round?.Start();
			}
		}
	}
}
