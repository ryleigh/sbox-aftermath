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
		private Button BuildButton;
		private Button ItemButton;
		private Panel HudBackgroundPanel;
		private Panel PersonBackgroundPanel;

		private Button AmmoButton;

		private Label ScrapLabel;
		private Label ScrapIcon;
		private Panel XpBar;

		public SelectionInfoPanel()
		{
			StyleSheet.Load( "UI/SelectionInfoPanel.scss" );

			HudBackgroundPanel = Add.Panel( "hudBackground" );

			PersonBackgroundPanel = HudBackgroundPanel.Add.Panel( "personBackground" );

			Panel buttonBackground = PersonBackgroundPanel.Add.Panel( "buttonBackground" );
			BuildButton = buttonBackground.AddChild<BuildButton>();
			BuildButton.SetClass( "button", true );
			BuildButton.AddEventListener( "onclick", BuildMode );
			BuildButton.Text = "construction";
			BuildButton.SetClass( "open", true );

			// ItemButton = buttonBackground.Add.Button( "", "itemButton" );
			ItemButton = buttonBackground.AddChild<ItemButton>( );
			ItemButton.SetClass( "button", true );
			ItemButton.AddEventListener( "onclick", () => {
				DropItem();
			} );

			AmmoButton = buttonBackground.AddChild<AmmoButton>();
			AmmoButton.SetClass( "ammoButton", true );
			AmmoButton.AddEventListener( "onclick", DropAmmo );
			AmmoButton.Text = "No Ammo";
			AmmoButton.SetClass( "open", false );

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

			BuildButton.SetClass( "toggledOn", player.IsBuildMode );

			if ( player.Selected.Count == 1 )
			{
				if ( player.Selected[0] is Person person )
				{
					NameLabel.Text = person.PersonName;
					ItemButton.Text = person.EquippedGun != null ? "auto_fix_normal" : "";
					ItemButton.SetClass( "open", person.EquippedGun != null );
					// AmmoButton.Text = person.AmmoType == AmmoType.None ? "t" : $"{person.AmmoAmount} {Person_AmmoHandler.GetDisplayName( person.AmmoType, person.AmmoAmount > 0 )}";
					AmmoButton.Text = person.AmmoType == AmmoType.None ? "" : $"{person.AmmoAmount} {Person_AmmoHandler.GetDisplayName( person.AmmoType, person.AmmoAmount > 0 )}";
					AmmoButton.SetClass( "open", person.AmmoType != AmmoType.None );
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

		public void BuildMode()
		{
			if ( Local.Pawn is not Player player ) return;
			player.ToggleBuildMode();
		}

		public void DropAmmo()
		{
			if ( Local.Pawn is not Player player ) return;
			if ( player.Selected.Count == 1 )
			{
				if ( player.Selected[0] is Person person )
				{
					Person.DropAmmo( person.NetworkIdent );
				}
			}
		}
	}

	public class BuildButton : Button
	{
		protected override void OnMouseOver( MousePanelEvent e )
		{
			if ( Local.Pawn is not Player player ) return;
			if ( player.Selected.Count == 1 )
			{
				if ( player.Selected[0] is Person person )
				{
					ItemTooltip.Instance.Update( "Build" );
					ItemTooltip.Instance.Hover( this );
					ItemTooltip.Instance.Show();
				}
			}
		}

		protected override void OnMouseOut( MousePanelEvent e )
		{
			ItemTooltip.Instance.Hide();
		}
	}

	public class AmmoButton : Button
	{
		protected override void OnMouseOver( MousePanelEvent e )
		{
			if ( Local.Pawn is not Player player ) return;
			if ( player.Selected.Count == 1 )
			{
				if ( player.Selected[0] is Person person )
				{
					ItemTooltip.Instance.Update( person.AmmoType == AmmoType.None ? "No Ammo" : $"{person.AmmoAmount} {Person_AmmoHandler.GetDisplayName( person.AmmoType, person.AmmoAmount > 0 )}" );
					ItemTooltip.Instance.Hover( this );
					ItemTooltip.Instance.Show();
				}
			}
		}

		protected override void OnMouseOut( MousePanelEvent e )
		{
			ItemTooltip.Instance.Hide();
		}
	}

	public class ItemButton : Button
	{
		protected override void OnMouseOver( MousePanelEvent e )
		{
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
			ItemTooltip.Instance.Hide();
		}
	}
}
