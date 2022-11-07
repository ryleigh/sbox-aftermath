using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace aftermath
{
	public class PlayerInfoHud : Panel
	{
		public Label Label;

		public PlayerInfoHud()
		{
			Label = Add.Label( "", "player_name" );
		}

		public override void Tick()
		{
			var player = Local.Pawn as Player;
			if ( player == null ) return;

			Label.Text = $"{player.Client.Name} (id: {player.Client.NetworkIdent}) - steam: {player.Client.PlayerId}, playerNum: {player.PlayerNum}";
			// Label.Style.FontColor = AftermathGame.Instance?.PlayerManager?.GetPlayerData( player.GetClientOwner().NetworkIdent )?.Color ?? Color.Black;
			Label.Style.FontColor = player.TeamColor;
			Label.Style.Dirty();

			// Panel.Style.Dirty()
		}
	}
}
