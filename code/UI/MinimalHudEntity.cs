using Sandbox;
using Sandbox.UI;

namespace aftermath
{
	public partial class MinimalHudEntity : HudEntity<RootPanel>
	{
		public static ItemTooltip Tooltip => ItemTooltip.Instance;

		public MinimalHudEntity()
		{
			if ( !IsClient ) return;

			RootPanel.SetTemplate( "/UI/minimalhud.html" );

			RootPanel.AddChild<PlayerInfoHud>();
			// RootPanel.AddChild<CursorController>();
			RootPanel.AddChild<ItemTooltip>();
		}
	}
}
