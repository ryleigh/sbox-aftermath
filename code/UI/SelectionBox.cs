﻿using Sandbox;
using Sandbox.UI;

namespace aftermath
{
	public class SelectionBox : Panel
	{
		public SelectionBox()
		{
			StyleSheet.Load( "/ui/SelectionBox.scss" );
		}

		public override void Tick()
		{
			base.Tick();

			if ( Local.Pawn is Player pawn )
			{
				Tick( pawn.FrustumSelect );
			}
		}

		private void Tick( FrustumSelect frustumSelect )
		{
			SetClass( "active", frustumSelect.IsDragging );
			if ( !frustumSelect.IsDragging ) return;

			var f = frustumSelect.GetFrustum( 1000 );

			var topleft = (f.GetCorner( 0 ) ?? Vector3.Zero).ToScreen();
			var bottomright = (f.GetCorner( 2 ) ?? Vector3.Zero).ToScreen();

			Style.Left = topleft.x * Screen.Width * ScaleFromScreen;
			Style.Top = topleft.y * Screen.Height * ScaleFromScreen;
			Style.Width = (bottomright.x - topleft.x) * Screen.Width * ScaleFromScreen;
			Style.Height = (bottomright.y - topleft.y) * Screen.Height * ScaleFromScreen;
			Style.Dirty();
		}
	}
}
