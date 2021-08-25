using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public partial class PlayRound : BaseRound
	{
		public override string RoundName => "PLAY";
		public override int RoundDuration => 0;
		public override bool ShowTimeLeft => true;

		public List<Player> Spectators = new();

		public override void OnPlayerJoin( Player player )
		{
			// player.MakeSpectator( true );
			Spectators.Add( player );

			base.OnPlayerJoin( player );
		}

		protected override void OnStart()
		{
			if ( Host.IsServer )
			{
				var players = Client.All.Select( ( client ) => client.Pawn as Player ).ToList();
				var colors = new List<Color>
				{
					Color.Red,
					Color.Blue,
					Color.Green,
					Color.Cyan,
					Color.Magenta,
					Color.Orange,
					Color.Yellow,
					Color.Gray
				};

				int playerNum = 1;
				foreach ( var player in players )
				{
					player.TeamColor = colors[0];
					colors.RemoveAt( 0 );
					player.PlayerNum = playerNum++;

					// player.MakeSpectator( false );
					AddPlayer( player );
				}
			}
		}

		protected override void OnFinish()
		{
			if ( Host.IsServer )
			{
				Spectators.Clear();
			}
		}
	}
}
