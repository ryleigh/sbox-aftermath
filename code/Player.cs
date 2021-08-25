using System;
using Sandbox;
using Sandbox.UI;
using System.Collections.Generic;
using System.Diagnostics;
using Trace = Sandbox.Trace;

namespace aftermath
{
	public partial class Player : ModelEntity
	{
		public FrustumSelect FrustumSelect = new FrustumSelect();

		[Net] public List<Entity> Selected { get; set; }

		[Net] public Color TeamColor { get; set; }
		[Net] public int PlayerNum { get; set; }

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

			var maxSpeed = 500;
			if ( Input.Down( InputButton.Run ) ) maxSpeed = 1000;

			Velocity += new Vector3( -Input.Left, Input.Forward, 0f ) * maxSpeed * 5 * Time.Delta;
			if ( Velocity.Length > maxSpeed ) Velocity = Velocity.Normal * maxSpeed;

			Velocity = Velocity.Approach( 0, Time.Delta * maxSpeed * 3 );

			Position += Velocity * Time.Delta;
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

			DebugOverlay.Line( new Vector3( 0f, 0f, 0.01f ), new Vector3( 1024f, 0f, 0.01f ), Color.Black );
			DebugOverlay.Line( new Vector3( 0f, 0f, 0.01f ), new Vector3( 0f, -1024f, 0.01f ), Color.Black );

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

				if ( Input.Released( InputButton.Slot1 ) )
				{
					AftermathGame.Instance.PersonManager.SpawnPersonServer( mouseWorldPos, this, PersonType.Survivor );
				}
				
				if ( Input.Released( InputButton.Slot2 ) )
				{
					AftermathGame.Instance.StructureManager.AddStructureServer( mouseGridPos, StructureType.Wall, Direction.Up );
				}

				if ( Input.Released( InputButton.Slot3 ) )
				{
					AftermathGame.Instance.PersonManager.SpawnPersonServer( mouseWorldPos, this, PersonType.Zombie);
				}

				if ( Input.Down( InputButton.Flashlight ) )
					AftermathGame.Instance.GridManager.HighlightGridSquare( mouseGridPos );
			}
		}

		// CLIENT
		[Event.BuildInput]
		public override void BuildInput( InputBuilder builder )
		{
			base.BuildInput( builder );

			if ( Input.Pressed( InputButton.Attack1 ) )
				FrustumSelect.Init( Input.Cursor, EyeRot );

			if ( Input.Down( InputButton.Attack1 ) )
				FrustumSelect.Update( Input.Cursor );

			if ( !Input.Down( InputButton.Attack1 ) )
				FrustumSelect.IsDragging = false;

			if ( Input.Pressed( InputButton.Attack1 ) )
			{
				FrustumSelect.Init( Input.Cursor, EyeRot );
			}

			if ( Input.Down( InputButton.Attack1 ) )
			{
				FrustumSelect.Update( Input.Cursor );

				if ( FrustumSelect.IsDragging )
				{
					foreach ( var entity in Selected )
					{
						if ( entity is Person person )
							person.Deselect();
					}

					Selected.Clear();

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

			Plane plane = new Plane( Vector3.Zero, new Vector3( 0f, 0f, 1f ) );
			Vector3? hitPos = plane.Trace( new Ray( Input.Cursor.Origin, Input.Cursor.Direction ), true, Double.PositiveInfinity );
			Vector2 mouseWorldPos = hitPos == null ? Vector2.Zero : new Vector2( hitPos.Value.x, hitPos.Value.y );

			if ( Input.Pressed( InputButton.Attack2 ) )
			{
				foreach ( var entity in Selected )
				{
					if ( entity is Survivor survivor )
					{
						Person.MoveTo( mouseWorldPos, survivor.NetworkIdent );
					}
				}
			}
		}

		// CLIENT
		public override void FrameSimulate( Client client )
		{
			base.FrameSimulate( client );

			DebugOverlay.ScreenText( 3, $"Num Players (Client): {Rounds.Current?.Players.Count}" );
			DebugOverlay.ScreenText( 4, $"Player: {this}" );

			DebugOverlay.ScreenText( 7, $"Selected (Client): {Selected.Count}" );
		}
	}
}
