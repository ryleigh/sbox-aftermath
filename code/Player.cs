using System;
using Sandbox;
using Sandbox.UI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Trace = Sandbox.Trace;

namespace aftermath
{
	public partial class Player : ModelEntity
	{
		public FrustumSelect FrustumSelect = new FrustumSelect();

		public List<Entity> Selected = new();
		public bool IsBuildMode { get; set; }
		public StructureType BuildModeType { get; set; }

		[Net] public Color TeamColor { get; set; }
		[Net] public int PlayerNum { get; set; }
		[Net] public int ScrapAmount { get; set; }

		public override void Spawn()
		{
			base.Spawn();

			Transmit = TransmitType.Always;
			Tags.Add( "player", "pawn" );
		}

		// SERVER & CLIENT
		public override void Simulate( Client client )
		{
			base.Simulate( client );

			Rotation = Rotation.LookAt( new Vector3( 0f, 0.075f, -1f ), new Vector3( 0f, 1f, 0f ) );
			EyeRot = Rotation;

			Plane plane = new Plane( Vector3.Zero, new Vector3( 0f, 0f, 1f ) );
			Vector3? hitPos = plane.Trace( new Ray( Input.Cursor.Origin, Input.Cursor.Direction ), true, Double.PositiveInfinity);
			Vector2 mouseWorldPos = hitPos == null ? Vector2.Zero : new Vector2( hitPos.Value.x, hitPos.Value.y );
			GridPosition mouseGridPos = AftermathGame.Instance.GridManager.GetGridPosFor2DPos( mouseWorldPos );

			if ( IsServer )
			{
				DebugOverlay.ScreenText( 0, $"# Entities: {Entity.All.Count}" );
				DebugOverlay.ScreenText( 1, $"# Clients: {Client.All.Count}" );

				DebugOverlay.ScreenText( 2, $"Num Players (Server): {Rounds.Current?.Players.Count}" );
				DebugOverlay.ScreenText( 5, $"Rounds.Current (Server): {Rounds.Current}" );
				DebugOverlay.ScreenText( 8, $"Selected (Server): {Selected.Count}" );

				if ( Input.Released( InputButton.Slot1 ) ) { AftermathGame.Instance.PersonManager.SpawnPersonServer( mouseWorldPos, this, PersonType.Survivor ); }
				if ( Input.Released( InputButton.Slot2 ) ) { AftermathGame.Instance.StructureManager.AddStructureServer( mouseGridPos, StructureType.Wall, Direction.Up ); }
				if ( Input.Released( InputButton.Slot3 ) ) { AftermathGame.Instance.PersonManager.SpawnPersonServer( mouseWorldPos, this, PersonType.Zombie); }
				if ( Input.Released( InputButton.Slot4 ) ) { AftermathGame.Instance.PersonManager.SpawnPersonServer( mouseWorldPos, this, PersonType.Soldier); }

				if ( Input.Released( InputButton.Slot5 ) )
				{
					// Log.Info( $"Person - spawn item at pos: {mouseWorldPos}" );

					AmmoItem ammoItem = new AmmoItem {Position = mouseWorldPos};
					ammoItem.SetPosition2D( new Vector2( mouseWorldPos.x, mouseWorldPos.y ) );

					switch ( Rand.Int(0, 3) )
					{
						case 0:
							ammoItem.Init( AmmoType.Bullet, 12 );
							break;
						case 1:
							ammoItem.Init( AmmoType.Shell, 10 );
							break;
						case 2:
							ammoItem.Init( AmmoType.HPBullet, 8);
							break;
						case 3:
							ammoItem.Init( AmmoType.Grenade, 3 );
							break;
					}

					// item.PlaceItem( item.Position + new Vector3( Rand.Float( -20f, 20f ), Rand.Float( -20f, 20f ), 0f ), 50f, 1f, 8 );
					// item.PlaceItem( item.Position + new Vector3( Rand.Float( -50f, 50f ), Rand.Float( -50f, 50f ), 0f ), Rand.Float( 50f, 100f ), Rand.Float( 0.5f, 1f ), 8);
					ammoItem.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 400f ), Rand.Float( 3f, 100f ), 8 );
				}

				if ( Input.Released( InputButton.Slot7 ) ) { ScrapAmount += 15; }
				if ( Input.Released( InputButton.Slot8 ) ) { ScrapAmount = 0; }

				if ( Input.Down( InputButton.Flashlight ) )
					AftermathGame.Instance.GridManager.HighlightGridSquare( mouseGridPos );
			}
			else
			{
				// CLIENT

				if ( Input.Pressed( InputButton.Attack1 ) )
				{
					FrustumSelect.Init( Input.Cursor, EyeRot );
				}
				
				if ( Input.Down( InputButton.Attack1 ) )
				{
					FrustumSelect.Update( Input.Cursor );
				
					if ( FrustumSelect.IsDragging )
					{
						DeselectAll();

						var f = FrustumSelect.GetFrustum();
				
						foreach ( var ent in Entity.All )
						{
							if ( !ent.Tags.Has( "selectable" ) ) continue;
							if ( !f.IsInside( ent.WorldSpaceBounds, true ) ) continue;
				
							if ( ent is Person person )
							{
								if ( !person.IsLocalPlayers ) continue;
								person.Select();
							}
				
							Selected.Add( ent );
						}
					}
				}
				
				if ( !Input.Down( InputButton.Attack1 ) )
					FrustumSelect.IsDragging = false;

				if ( Input.Pressed( InputButton.Attack2 ) )
				{
					foreach ( var entity in Selected )
					{
						if ( entity is Survivor survivor )
						{
							Person.MoveTo( mouseWorldPos, survivor.NetworkIdent );
							// GridPosition gridPos = AftermathGame.Instance.GridManager.GetGridPosFor2DPos( mouseWorldPos );
							// Person.MoveToBuild( mouseWorldPos, gridPos.X, gridPos.Y, StructureType.Wall, Direction.Up, 0, survivor.NetworkIdent );
						}
					}
				}

				if ( Input.Pressed( InputButton.Slot6 ) )
				{
					foreach ( var entity in Selected )
					{
						if ( entity is Survivor survivor )
						{
							Person.DropGun( survivor.NetworkIdent );
						}
					}
				}

				// if ( Input.Pressed( InputButton.Slot8 ) )
				// {
				// 	foreach ( var entity in Selected )
				// 	{
				// 		if ( entity is Survivor survivor )
				// 		{
				// 			Person.Attack( survivor.NetworkIdent );
				// 			// Person.DropGun( survivor.NetworkIdent );
				// 		}
				// 	}
				// }

				var trace = Utils.TraceRayDirection( Input.Cursor.Origin, Input.Cursor.Direction ).EntitiesOnly().Radius( 10f ).Run();
				bool showTooltip = false;

				if ( trace.Entity is Person p )
				{
					ItemTooltip.Instance.Update( p );
					ItemTooltip.Instance.Hover( p );
					ItemTooltip.Instance.Show();
					showTooltip = true;

					if ( Input.Pressed( InputButton.Attack1 ) )
					{
						if ( !Input.Down( InputButton.Run ) )
							DeselectAll();
					
						if ( !p.IsDead && p.Tags.Has( "selectable" ) && p.IsLocalPlayers && p is Survivor )
						{
							p.Select();
							Selected.Add( p );
						}
					}
				}
				else 
				{
					if ( Input.Pressed( InputButton.Attack1 ) )
					{
						if ( IsBuildMode && BuildModeType != StructureType.None )
						{
							if ( Selected.Count == 1 )
							{
								foreach ( var entity in Selected )
								{
									if ( entity is Survivor survivor )
									{
										GridPosition gridPos = AftermathGame.Instance.GridManager.GetGridPosFor2DPos( mouseWorldPos );
										Person.MoveToBuild( mouseWorldPos, gridPos.X, gridPos.Y, BuildModeType, Direction.Up, Structure.GetCost( BuildModeType ), survivor.NetworkIdent );
										// Person.DropGun( survivor.NetworkIdent );
										ToggleBuildMode();
									}
								}
							}
						}
						else
						{
							DeselectAll();
						}
					}
					
					if ( trace.Entity is Item item )
					{
						if ( item is Gun {IsHeld: true} )
						{
							// do nothing
						}
						else
						{
							ItemTooltip.Instance.Update( item );
							ItemTooltip.Instance.Hover( item );
							ItemTooltip.Instance.Show();
							showTooltip = true;

							if ( Input.Pressed( InputButton.Attack2 ) )
							{
								foreach ( var entity in Selected )
								{
									if ( entity is Survivor survivor )
									{
										Person.MoveToPickUpItem( item.NetworkIdent, survivor.NetworkIdent );
									}
								}
							}
						}
					}
				}

				if ( !showTooltip && !ItemTooltip.Instance.IsOnHud )
					ItemTooltip.Instance.Hide();
			}

			HandleMovement( Time.Delta );
		}

		void HandleMovement( float dt )
		{
			var maxSpeed = 500;
			if ( Input.Down( InputButton.Run ) ) maxSpeed = 1000;

			Velocity += new Vector3( -Input.Left, Input.Forward, 0f ) * maxSpeed * 5 * dt;
			if ( Velocity.Length > maxSpeed ) Velocity = Velocity.Normal * maxSpeed;

			Velocity = Velocity.Approach( 0, Time.Delta * maxSpeed * 3 );

			Position += Velocity * dt;
			Position = new Vector3( Position.x, Position.y, 700f );

			float X_MIN = -500f;
			float X_MAX = 500f;
			float Y_MIN = -734f;
			float Y_MAX = 734f;

			if ( Position.x < X_MIN )
			{
				Position = new Vector3( X_MIN, Position.y, Position.z );
				Velocity = new Vector3( 0f, Velocity.y, Velocity.z );
			}
			else if ( Position.x > X_MAX )
			{
				Position = new Vector3( X_MAX, Position.y, Position.z );
				Velocity = new Vector3( 0f, Velocity.y, Velocity.z );
			}

			if ( Position.y < Y_MIN )
			{
				Position = new Vector3( Position.x, Y_MIN, Position.z );
				Velocity = new Vector3( Velocity.x, 0f, Velocity.z );
			}
			else if ( Position.y > Y_MAX )
			{
				Position = new Vector3( Position.x, Y_MAX, Position.z );
				Velocity = new Vector3( Velocity.x, 0f, Velocity.z );
			}

			EyePos = Position;

			// DebugOverlay.Line( new Vector3( 0f, 0f, 0.01f ), new Vector3( 1024f, 0f, 0.01f ), Color.Black );
			// DebugOverlay.Line( new Vector3( 0f, 0f, 0.01f ), new Vector3( 0f, -1024f, 0.01f ), Color.Black );
		}

		// CLIENT
		[Event.BuildInput]
		public override void BuildInput( InputBuilder builder )
		{
			base.BuildInput( builder );

		}

		public void DeselectAll()
		{
			// Log.Info( "Deselect All" );

			foreach ( Entity entity in Selected )
			{
				if ( entity is Person person )
				{
					person.Deselect();
				}
			}

			Selected.Clear();

			IsBuildMode = false;
			SelectBuildType( StructureType.None );
		}

		public void Select( ISelectable selectable, bool isAdditive )
		{
			if( !isAdditive ) DeselectAll();

			if ( selectable is Person {IsLocalPlayers: true, IsSelected:false} person)
			{
				person.Select();
				Selected.Add( person );
			}
		}

		// CLIENT
		public override void FrameSimulate( Client client )
		{
			base.FrameSimulate( client );

			DebugOverlay.ScreenText( 3, $"Num Players (Client): {Rounds.Current?.Players.Count}" );
			DebugOverlay.ScreenText( 4, $"Player: {this}" );

			DebugOverlay.ScreenText( 7, $"Selected (Client): {Selected.Count}" );
			DebugOverlay.ScreenText( 11, $"IsBuildMode (Client): {IsBuildMode}" );
			DebugOverlay.ScreenText( 12, $"BuildModeType: {BuildModeType}" );

			if ( _buildingIndicator != null )
			{
				Plane plane = new Plane( Vector3.Zero, new Vector3( 0f, 0f, 1f ) );
				Vector3? hitPos = plane.Trace( new Ray( Input.Cursor.Origin, Input.Cursor.Direction ), true, Double.PositiveInfinity );
				Vector2 mouseWorldPos = hitPos == null ? Vector2.Zero : new Vector2( hitPos.Value.x, hitPos.Value.y );
				GridPosition mouseGridPos = AftermathGame.Instance.GridManager.GetGridPosFor2DPos( mouseWorldPos );

				_buildingIndicator.Position = AftermathGame.Instance.GridManager.Get2DPosForGridPos( mouseGridPos );
				_buildingIndicator.RenderColor = new Color( 0.85f, 0.85f, 1f, 0.4f + MathF.Sin( Time.Now * 8f ) * 0.2f );
				// _buildingIndicator.GlowColor = new Color( 0.1f, 0.1f, 1f, 0f + MathF.Sin( Time.Now * 5f ) * 0.4f );
				// _buildingIndicator.GlowActive = true;
			}
			
		}

		public void ToggleBuildMode()
		{
			Host.AssertClient();

			IsBuildMode = !IsBuildMode;
			SelectBuildType( StructureType.None );
		}

		private BuildingIndicator _buildingIndicator;

		public void SelectBuildType( StructureType structureType )
		{
			Host.AssertClient();

			BuildModeType = structureType;

			if ( _buildingIndicator != null )
			{
				_buildingIndicator.Delete();
				_buildingIndicator = null;
			}


			if ( structureType != StructureType.None )
			{
				_buildingIndicator = new BuildingIndicator
				{
					RenderColor = new Color( 0f, 0f, 0f, 0f )
				};
			}
		}

		public void AdjustScrapAmount( int amount )
		{
			ScrapAmount += amount;
		}
	}
}
