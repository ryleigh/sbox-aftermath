using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;


namespace aftermath
{
	public partial class SelectionInfoPanel : Panel
	{
		private Label NameLabel;
		private Button ItemButton;

		private Label XpLabel;
		private Panel XpBar;

		public SelectionInfoPanel()
		{
			StyleSheet.Load( "UI/SelectionInfoPanel.scss" );

			Panel background = Add.Panel( "background" );

			NameLabel = background.Add.Label( "", "nameText" );

			Panel healthIconBack = background.Add.Panel( "healthIconBack" );
			ItemButton = healthIconBack.Add.Button( "", "itemButton" );
			ItemButton.AddEventListener( "onclick", () => {
				DropItem();
			} );

			// Panel healthBarBack = background.Add.Panel( "healthBarBack" );
			//
			// XpBar = healthBarBack.Add.Panel( "healthBar" );
			// XpLabel = background.Add.Label( "0", "healthText" );
		}

		public override void Tick()
		{
			base.Tick();

			if (Local.Pawn is not Player player) return;

			// XpLabel.Text = player.Position.ToString();

			// XpBar.Style.Width = Length.Percent( 50f + (float)Math.Sin( Time.Now ) * 50f );
			// XpBar.Style.Dirty();

			if ( player.Selected.Count == 1 )
			{
				if ( player.Selected[0] is Person person )
				{
					NameLabel.Text = person.PersonName;
					ItemButton.Text = person.EquippedGun != null ? "get_app" : "";
				}

				SetClass( "open", true );
			}
			else
			{
				SetClass( "open", false );
			}
		}

		public void DropItem()
		{
			if ( Local.Pawn is not Player player ) return;
			if ( player.Selected.Count == 1 )
			{
				if ( player.Selected[0] is Person person )
				{
					Person.DropGun( person.NetworkIdent );
				}
			}
		}
	}
}
