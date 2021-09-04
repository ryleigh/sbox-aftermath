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
		private Panel ScrapBackgroundPanel;

		private Label NameLabel;
		private Button ItemButton;
		private Panel HudBackgroundPanel;
		private Panel PersonBackgroundPanel;

		private Label ScrapLabel;
		private Label ScrapIcon;
		private Panel XpBar;

		public SelectionInfoPanel()
		{
			StyleSheet.Load( "UI/SelectionInfoPanel.scss" );

			HudBackgroundPanel = Add.Panel( "hudBackground" );

			PersonBackgroundPanel = HudBackgroundPanel.Add.Panel( "personBackground" );

			Panel buttonBackground = PersonBackgroundPanel.Add.Panel( "itemButtonBackground" );
			// ItemButton = buttonBackground.Add.Button( "", "itemButton" );
			ItemButton = buttonBackground.AddChild<ItemButton>( );
			ItemButton.SetClass( "itemButton", true );
			ItemButton.AddEventListener( "onclick", () => {
				DropItem();
			} );

			NameLabel = PersonBackgroundPanel.Add.Label( "", "nameText" );

			ScrapLabel = HudBackgroundPanel.Add.Label( "0", "scrapText" );
			ScrapIcon = HudBackgroundPanel.Add.Label( "clear_all", "scrapIcon" );

			// Panel healthBarBack = background.Add.Panel( "healthBarBack" );
			//
			// XpBar = healthBarBack.Add.Panel( "healthBar" );
		}

		public override void Tick()
		{
			base.Tick();

			if (Local.Pawn is not Player player) return;

			ScrapLabel.Text = player.ScrapAmount.ToString();

			// XpLabel.Text = player.Position.ToString();

			// XpBar.Style.Width = Length.Percent( 50f + (float)Math.Sin( Time.Now ) * 50f );
			// XpBar.Style.Dirty();

			if ( player.Selected.Count == 1 )
			{
				if ( player.Selected[0] is Person person )
				{
					NameLabel.Text = person.PersonName;
					ItemButton.Text = person.EquippedGun != null ? "get_app" : "";
					ItemButton.SetClass( "open", person.EquippedGun != null );

				}

				PersonBackgroundPanel.SetClass( "open", true );
			}
			else
			{
				PersonBackgroundPanel.SetClass( "open", false );
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

	public class ItemButton : Button
	{
		protected override void OnMouseOver( MousePanelEvent e )
		{
			Log.Info( $"OVER!!! {Time.Now}" );
			if ( Local.Pawn is not Player player ) return;
			if ( player.Selected.Count == 1 )
			{
				if ( player.Selected[0] is Person person )
				{
					if ( person.EquippedGun != null )
					{
						ItemTooltip.Instance.Update( person.EquippedGun );
						ItemTooltip.Instance.Hover( this );
						ItemTooltip.Instance.Show();
					}
				}
			}
		}

		protected override void OnMouseOut( MousePanelEvent e )
		{
			Log.Info( $"OUT! {Time.Now}" );
			ItemTooltip.Instance.Hide();
		}
	}
}
