using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace aftermath
{
    public class ItemTooltip : Panel
    {
        public static ItemTooltip Instance { get; private set; }

        public Label Name { get; private set; }
        public Label Desc { get; private set; }
        public bool IsShowing { get; private set; }
        public object Target { get; private set; }
        public bool IsOnHud { get; private set; }

		public ItemTooltip()
        {
	        StyleSheet.Load( "/UI/ItemTooltip.scss" );

	        Name = Add.Label( "", "name" );
	        Desc = Add.Label("", "name");

	        Instance = this;
        }

        public void Show()
        {
	        IsShowing = true;
        }

        public void Hide()
        {
	        IsShowing = false;
	        IsOnHud = false;
        }

        public void Hover( Entity entity )
        {
	        Target = entity;
	        UpdatePosition();
        }

        public void Hover( Panel panel )
        {
	        Target = panel;
	        IsOnHud = true;
	        UpdatePosition();
        }

		public void Update( Person person )
        {
	        Name.Text = person.PersonName;
	        Desc.Text = person.Position2D.ToString();
        }

        public void Update( Item item )
        {
	        Name.Text = item.GetHoverInfo();
	        Desc.Text = item.Position2D.ToString();
        }

        public override void Tick()
        {
	        SetClass("hidden", !IsShowing );

	        if ( IsShowing )
		        UpdatePosition();

			base.Tick();
        }

        private void UpdatePosition()
        {
	        if ( Target is Panel panel )
	        {
		        var targetBox = panel.Box.Rect * ScaleFromScreen;

		        Style.Left = Length.Pixels( targetBox.Center.x );
		        Style.Top = Length.Pixels( targetBox.top - 32 );
		        Style.Dirty();
	        }
			else if ( Target is Entity entity && entity.IsValid() )
	        {
		        var position = entity.Position.ToScreen() * new Vector3( Screen.Width, Screen.Height ) * ScaleFromScreen;

		        Style.Left = Length.Pixels( position.x );
		        Style.Top = Length.Pixels( position.y - 32 );
				Style.Dirty();
	        }
        }
    }
}
