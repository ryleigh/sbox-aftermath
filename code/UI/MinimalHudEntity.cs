using Sandbox;
using Sandbox.UI;

namespace aftermath
{
	public partial class MinimalHudEntity : HudEntity<RootPanel>
	{
		public MinimalHudEntity()
		{
			if ( !IsClient ) return;

			RootPanel.SetTemplate( "/UI/minimalhud.html" );

			RootPanel.AddChild<PlayerInfoHud>();
		}
	}
}
