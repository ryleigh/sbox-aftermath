using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;


namespace aftermath
{
	public partial class BuildMenuPanel : Panel
	{


		private Panel ScrapBackgroundPanel;

		private List<BuildingButton> _buildingButtons = new();
		private Panel HudBackgroundPanel;
		private Panel PersonBackgroundPanel;

		public BuildMenuPanel()
		{
			StyleSheet.Load( "UI/BuildMenuPanel.scss" );

			HudBackgroundPanel = Add.Panel( "hudBackground" );

			PersonBackgroundPanel = HudBackgroundPanel.Add.Panel( "personBackground" );

			AddBuildingButton( StructureType.Wall );
			AddBuildingButton( StructureType.Fence );
			AddBuildingButton( StructureType.AmmoCacheBullets );
			AddBuildingButton( StructureType.AmmoCacheShells);
			AddBuildingButton( StructureType.AmmoCacheHPBullets );
			AddBuildingButton( StructureType.Turret );
			AddBuildingButton( StructureType.Factory );

			// Panel buttonBackground = PersonBackgroundPanel.Add.Panel( "buttonBackground" );
			//
			// WallButton = buttonBackground.AddChild<BuildingButton>();
			// WallButton.SetClass( "button", true );
			// WallButton.AddEventListener( "onclick", () =>
			// {
			// 	BuildingSelected( StructureType.Wall );
			// } );
			// WallButton.SetClass( "open", true );
			// WallButton.Name = "Wall";
			// WallButton.Cost = Structure.GetCost( StructureType.Wall );
		}

		public override void Tick()
		{
			base.Tick();

			if ( Local.Pawn is not Player player ) return;

			HudBackgroundPanel.SetClass( "open", player.IsBuildMode );
			PersonBackgroundPanel.SetClass( "open", player.IsBuildMode );

			foreach ( BuildingButton button in _buildingButtons )
			{
				button.SetClass( "tooExpensive", button.Cost > player.ScrapAmount );
				button.SetClass( "selected", player.IsBuildMode && player.BuildStructureType == button.StructureType );
			}
		}

		void AddBuildingButton( StructureType structureType )
		{
			Panel buttonBackground = PersonBackgroundPanel.Add.Panel( "buttonBackground" );

			BuildingButton button = buttonBackground.AddChild<BuildingButton>();
			button.SetClass( "button", true );
			button.AddEventListener( "onclick", () =>
			{
				BuildingSelected( structureType );
			} );
			button.SetClass( "open", true );
			button.StructureType = structureType;
			button.Name = Structure.GetBuildingName( structureType );
			button.Cost = Structure.GetCost( structureType );
			button.Text = Structure.GetBuildingIcon( structureType );

			_buildingButtons.Add( button );
		}

		public void BuildingSelected(StructureType structureType)
		{
			if ( Local.Pawn is not Player player ) return;

			int cost = Structure.GetCost( structureType );

			if ( cost > player.ScrapAmount )
			{
				if(player.Selected.Count > 0)
					AftermathGame.Instance.SpawnFloater( player.Selected[0].Position, $"Need more scrap for {Structure.GetBuildingName( structureType )}!", new Color( 1f, 0.2f, 0.1f, 0.5f ) );
			}
			else
			{
				player.SelectBuildType( structureType );
			}
		}
	}

	public class BuildingButton : Button
	{
		public StructureType StructureType { get; set; }
		public string Name { get; set; }
		public int Cost { get; set; }

		protected override void OnMouseOver( MousePanelEvent e )
		{
			if ( Local.Pawn is not Player player ) return;
			if ( player.Selected.Count == 1 )
			{
				if ( player.Selected[0] is Person person )
				{
					ItemTooltip.Instance.Update( Name, Cost + " Scrap", negativeDesc: Cost > player.ScrapAmount);
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
}
