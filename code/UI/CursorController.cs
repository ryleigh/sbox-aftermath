using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

namespace aftermath
{
	public class CursorController : Panel
	{
		public Vector2 StartSelection { get; private set; }
		public Panel SelectionArea { get; private set; }
		public Rect SelectionRect { get; private set; }
		public bool IsSelecting { get; private set; }
		public bool IsMultiSelect { get; private set; }

		public CursorController()
		{
			StyleSheet.Load( "/UI/CursorController.scss" );
			SelectionArea = Add.Panel( "selection" );
		}

		public override void Tick()
		{
			SelectionArea.SetClass( "hidden", !IsSelecting || !IsMultiSelect);

			base.Tick();
		}

		[Event.BuildInput]
		private void BuildInput( InputBuilder builder )
		{
			if(Local.Pawn is not Player player)
				return;

			if ( builder.Pressed( InputButton.Attack1 ) )
			{
				StartSelection = Mouse.Position;
				IsMultiSelect = false;
				IsSelecting = true;
			}

			if ( builder.Released( InputButton.Attack2 ) )
			{
				if ( player.Selected.Count > 0 )
				{
					var trace = Utils.TraceRayDirection( builder.Cursor.Origin, builder.Cursor.Direction )
						.Radius( 5f )
						.Run();

					if ( trace.Entity is ISelectable selectable )
					{
						// attack etc
					}
					else
					{
						foreach ( var entity in player.Selected )
						{
							if ( entity is Survivor survivor )
							{
								Person.MoveTo( trace.EndPos, survivor.NetworkIdent );
							}
						}
					}
				}
			}

			if ( builder.Down( InputButton.Attack1 ) && IsSelecting )
			{
				var position = Mouse.Position;
				var selection = new Rect(
					Math.Min( StartSelection.x, position.x ),
					Math.Min( StartSelection.y, position.y ),
					Math.Abs( StartSelection.x - position.x ),
					Math.Abs( StartSelection.y - position.y )
				);
				SelectionArea.Style.Left = Length.Pixels( selection.left * ScaleFromScreen );
				SelectionArea.Style.Top = Length.Pixels( selection.top * ScaleFromScreen );
				SelectionArea.Style.Width = Length.Pixels( selection.width * ScaleFromScreen );
				SelectionArea.Style.Height = Length.Pixels( selection.height * ScaleFromScreen );
				SelectionArea.Style.Dirty();

				IsMultiSelect = (selection.width > 1f || selection.height > 1f);
				SelectionRect = selection;
			} 
			else if ( IsSelecting )
			{
				if ( IsMultiSelect )
				{
					var selectable = Entity.All.OfType<ISelectable>();

					foreach ( var b in selectable )
					{
						if ( b is Person {IsDead: false, IsSelected: false} person )
						{
							var screenScale = person.Position.ToScreen();
							var screenX = Screen.Width * screenScale.y;
							var screenY = Screen.Height * screenScale.y;

							if ( SelectionRect.IsInside( new Rect( screenX, screenY, 1f, 1f ) ) )
							{
								player.Select( person, isAdditive: true );
							}
						}
					}
				}
				else
				{
					var trace = Utils.TraceRayDirection( builder.Cursor.Origin, builder.Cursor.Direction ).EntitiesOnly().Run();

					if ( trace.Entity is ISelectable selectable )
					{
						player.Select( selectable, isAdditive: Input.Down( InputButton.Run ));
					}
					else
					{
						player.DeselectAll();
					}
				}

				IsSelecting = false;
			}
			else
			{
				var trace = Utils.TraceRayDirection( builder.Cursor.Origin, builder.Cursor.Direction ).EntitiesOnly().Run();

				// hover tooltip
			}
		}
	}
}
