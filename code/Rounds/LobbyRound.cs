using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class LobbyRound : BaseRound
	{
		public override string RoundName => "LOBBY";
		public override bool ShowRoundInfo => true;

		protected override void OnStart()
		{
			if ( Host.IsServer )
			{
				var players = Client.All.Select( ( client ) => client.Pawn as Player );

				foreach ( var player in players )
					OnPlayerJoin( player );
			}
		}

		public override void OnPlayerJoin( Player player )
		{
			if ( Players.Contains( player ) ) 
				return;

			// player.MakeSpectator( true );

			Log.Info( $"LobbyRound - OnPlayerJoin: {player}" );
			player.TeamColor = Color.White;
			player.PlayerNum = 1;

			AddPlayer( player );

			base.OnPlayerJoin( player );
		}
	}
}
