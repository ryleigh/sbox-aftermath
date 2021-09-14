using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace aftermath
{
	public enum Direction { None, Up, Right, Down, Left }
	public enum RaycastMode { None, Movement, Sight, Gunshot }

	public partial class GridManager : Entity
	{
		public int GridWidth { get; private set; }
		public int GridDepth { get; private set; }
		public int GridXMin => -(GridWidth / 2f).FloorToInt();
		public int GridXMax => (GridWidth / 2f).FloorToInt();
		public int GridYMin => -(GridDepth / 2f).FloorToInt();
		public int GridYMax => (GridDepth / 2f).FloorToInt();

		public float ArenaWidth { get; private set; }
		public float ArenaDepth { get; private set; }
		public float ArenaXMin => -ArenaWidth / 2f;
		public float ArenaXMax => ArenaWidth / 2f;
		public float ArenaYMin => -ArenaDepth / 2f;
		public float ArenaYMax => ArenaDepth / 2f;

		public float SquareSize { get; } = 64f;
		public float SquareHeight { get; } = 72f;

		readonly float MOVE_POS_WALL_BUFFER = 30f;

		public GridManager()
		{
			Transmit = TransmitType.Always;
			// Log.Warning( $"GridManager ctor **************** IsServer: {Host.IsServer}," );
		}

		public void Initialize( int gridWidth, int gridDepth )
		{
			GridWidth = gridWidth;
			GridDepth = gridDepth;
			ArenaWidth = (float)gridWidth * SquareSize;
			ArenaDepth = (float)gridDepth * SquareSize;

			// Log.Warning( $"GridManager Initialize **************** IsServer: {Host.IsServer}, GridWidth: {GridWidth}, GridDepth: {GridDepth}, SquareSize: {SquareSize}" );
		}

		public GridPosition GetGridPosFor2DPos( Vector2 pos2D )
		{
			return new GridPosition(
				(pos2D.x / SquareSize).FloorToInt() + GridWidth / 2,
				(pos2D.y / SquareSize).FloorToInt() + GridDepth / 2
			);
		}

		public Vector3 GetWorldPosForGridPos( GridPosition gridPos )
		{
			return new Vector3(
					(gridPos.X - (GridWidth / 2f).FloorToInt() + 0.5f) * SquareSize,
					(gridPos.Y - (GridWidth / 2f).FloorToInt() + 0.5f) * SquareSize,
					0f
			);
		}

		public Vector2 Get2DPosForGridPos( GridPosition gridPos )
		{
			return new Vector2(
					gridPos.X - (GridWidth / 2f).FloorToInt() + 0.5f,
					gridPos.Y - (GridWidth / 2f).FloorToInt() + 0.5f
			) * SquareSize;
		}

		public Vector3 GetWorldPosFor2DPos( Vector2 pos2D )
		{
			return new Vector3( pos2D.x, pos2D.y, 0f );
		}

		public bool IsGridPosInBounds( GridPosition gridPos )
		{
			return
				gridPos.X >= 0 &&
				gridPos.X < GridWidth &&
				gridPos.Y >= 0 &&
				gridPos.Y < GridDepth;
		}

		public int GetIndexForGridPos( GridPosition gridPos ) { return gridPos.Y * GridWidth + gridPos.X; }
		public GridPosition GetGridPosForIndex( int index ) { return new GridPosition( index % GridWidth, ((float)index / (float)GridWidth).FloorToInt() ); }

		public GridPosition GetRandomEmptyGridPos()
		{
			int emptyIndex = AftermathGame.Instance.StructureManager.GetRandomEmptyIndex();

			if ( emptyIndex >= 0 )
			{
				return GetGridPosForIndex( emptyIndex );
			}

			return GridPosition.Invalid;
		}

		public bool GetRandomEmpty2DPos( out Vector2 pos )
		{
			GridPosition gridPos = GetRandomEmptyGridPos();
			if ( gridPos.IsValid )
			{
				pos = Get2DPosForGridPos( gridPos );
				return true;
			}

			pos = Vector2.Zero;
			return false;
		}

		public bool GetRandomEmptyEdgePos( out Vector2 pos )
		{
			int emptyIndex = AftermathGame.Instance.StructureManager.GetRandomEmptyEdgeIndex();

			if ( emptyIndex >= 0 )
			{
				GridPosition gridPos = GetGridPosForIndex( emptyIndex );
				pos = Get2DPosForGridPos( gridPos );
				return true;
			}

			pos = Vector2.Zero;
			return false;
		}

		public bool IsEdgeGridPos( GridPosition gridPos )
		{
			return gridPos.X == 0 || gridPos.Y == 0 || gridPos.X == GridWidth - 1 || gridPos.Y == GridDepth - 1;
		}

		public GridPosition GetAdjacentEmptyGridPosition( GridPosition gridPos )
		{
			List<Direction> directions = Enum.GetValues( typeof( Direction ) ).Cast<Direction>().ToList();
			directions.Shuffle();
			foreach ( Direction direction in directions )
			{
				if ( direction == Direction.None )
					continue;
			
				GridPosition adjacent = GetGridPosInDirection( gridPos, direction );
				if ( adjacent.IsValid && !AftermathGame.Instance.StructureManager.IsStructure( adjacent ) )
					return adjacent;
			}

			return GridPosition.Invalid;
		}

		public bool Raycast( Vector2 a, Vector2 b, RaycastMode mode, out GridPosition o_gridPos, out Vector2 o_hitPos, out Vector2 o_normal )
		{
			GridPosition currGridPos = GetGridPosFor2DPos( a );
			o_gridPos = currGridPos;
			o_normal = Vector2.Zero;
			o_hitPos = a;
			
			if ( a.Equals( b ) )
				return false;
			
			if ( !currGridPos.IsValid || !IsGridPosInBounds( currGridPos ) )
				return false;
			
			// Maximum time the ray has traveled so far (not distance!)
			float tMaxX, tMaxY;
			// The time that the ray needs to travel to cross a single tile (not distance!)
			float tDeltaX = 0f;
			float tDeltaY = 0f;

			float sqrDist = (b - a).LengthSquared;
			Vector2 dir = (b - a).Normal;

			// Step direction, either 1 or -1
			var stepX = (dir.x > 0f) ? 1 : -1;
			var stepY = (dir.y > 0f) ? 1 : -1;

			// Starting tile cell bounds
			var startBoundsX = Get2DPosForGridPos( currGridPos ).x + (SquareSize / 2f) * stepX;
			var startBoundsY = Get2DPosForGridPos( currGridPos ).y + (SquareSize / 2f) * stepY;
			
			if ( Math.Abs( dir.x ) > 0f )
			{
				tMaxX = (startBoundsX - a.x) / dir.x;
				tDeltaX = SquareSize * stepX / dir.x;
			}
			else
			{
				tMaxX = float.MaxValue;
			}
			
			if ( Math.Abs( dir.y ) > 0f )
			{
				tMaxY = (startBoundsY - a.y) / dir.y;
				tDeltaY = SquareSize * stepY / dir.y;
			}
			else
			{
				tMaxY = float.MaxValue;
			}
			
			// loop through grid squares as you move in the direction of the line
			// figure out whether you'll leave the current square horizontally or vertically first,
			// then change squares and adjust next bounds requirement accordingly
			while ( currGridPos.IsValid && IsGridPosInBounds( currGridPos ) )
			{
				//                int index = GetIndexForGridPos(currGridPos);
			
				if ((mode == RaycastMode.Movement && IsMovementBlockingStructure( currGridPos )) ||
					(mode == RaycastMode.Sight && IsSightBlockingStructure( currGridPos )) ||
					(mode == RaycastMode.Gunshot && IsGunshotBlockingStructure( currGridPos )) )
				{
					o_gridPos = currGridPos;
					return true;
				}
			
				float tResult = (tMaxX < tMaxY) ? tMaxX : tMaxY;
				if ( Math.Pow( tResult, 2f ) > sqrDist )
				{
					o_hitPos = b;
					return false;
				}
			
				if ( tMaxX < tMaxY )
				{
					currGridPos = new GridPosition( currGridPos.X + stepX, currGridPos.Y );
					o_normal = (dir.x < 0f) ? Vector2.Right : -Vector2.Right;
					o_hitPos = a + dir * tMaxX;
			
					tMaxX += tDeltaX;
				}
				else
				{
					currGridPos = new GridPosition( currGridPos.X, currGridPos.Y + stepY );
					o_normal = (dir.y < 0f) ? Vector2.Up: -Vector2.Up;
					o_hitPos = a + dir * tMaxY;
			
					tMaxY += tDeltaY;
				}
			}

			return false;
		}

		public int GetGridDistance( GridPosition a, GridPosition b )
		{
			return Math.Abs( b.X - a.X ) + Math.Abs( b.Y - a.Y );
		}

		public GridPosition GetGridPosInDirection( GridPosition gridPos, Direction direction )
		{
			GridPosition newGridPos = GridPosition.Invalid;

			if ( direction == Direction.Left )
				newGridPos = new GridPosition( gridPos.X - 1, gridPos.Y );
			else if ( direction == Direction.Right )
				newGridPos = new GridPosition( gridPos.X + 1, gridPos.Y );
			else if ( direction == Direction.Down )
				newGridPos = new GridPosition( gridPos.X, gridPos.Y - 1 );
			else if ( direction == Direction.Up )
				newGridPos = new GridPosition( gridPos.X, gridPos.Y + 1 );

			return IsGridPosInBounds( newGridPos ) ? newGridPos : GridPosition.Invalid;
		}

		public bool IsWalkable( GridPosition gridPos )
		{
			if ( !gridPos.IsValid ||
				gridPos.X < 0 || gridPos.X > GridWidth - 1 ||
				gridPos.Y < 0 || gridPos.Y > GridDepth - 1 ||
				IsMovementBlockingStructure( gridPos ) )
				return false;

			return true;
		}

		public bool IsMovementBlockingStructure( GridPosition gridPos )
		{
			Structure structure = AftermathGame.Instance.StructureManager.GetStructure( GetIndexForGridPos( gridPos ) );
			return structure is {BlocksMovement: true};
		}

		public bool IsSightBlockingStructure( GridPosition gridPos )
		{
			Structure structure = AftermathGame.Instance.StructureManager.GetStructure( GetIndexForGridPos( gridPos ) );
			return structure is {BlocksSight: true};
		}

		public bool IsGunshotBlockingStructure( GridPosition gridPos )
		{
			Structure structure = AftermathGame.Instance.StructureManager.GetStructure( GetIndexForGridPos( gridPos ) );
			return structure is { BlocksGunshots: true };
		}

		public bool DoesGridPosContainPerson( GridPosition gridPos )
		{
			// return DoesPersonExistInGridPos( gridPos, GameMode.PersonManager.People );

			return false;
		}

		public bool DoesGridPosContainSurvivor( GridPosition gridPos )
		{
			// return DoesPersonExistInGridPos( gridPos, GameMode.PersonManager.Survivors );

			return false;
		}

		public bool DoesGridPosContainZombie( GridPosition gridPos )
		{
			// return DoesPersonExistInGridPos( gridPos, GameMode.PersonManager.Zombies );

			return false;
		}

		public bool DoesPersonExistInGridPos( GridPosition gridPos, List<Person> people )
		{
			if ( !gridPos.IsValid || !IsGridPosInBounds( gridPos ) )
				return false;
		
			// foreach ( Person person in people )
			// {
			// 	if ( !person.IsDead && person.Movement.CurrentGridPos == gridPos )
			// 	{
			// 		return true;
			// 	}
			// }
		
			return false;
		}

		public Vector2 GetRandomPos2DInGridPos( GridPosition gridPos )
		{
			Vector2 pos = Get2DPosForGridPos( gridPos ) + new Vector2( Rand.Float( -SquareSize * 0.5f, SquareSize * 0.5f ), Rand.Float( -SquareSize * 0.5f, SquareSize * 0.5f ) );
			ImproveMovePosition( ref pos );
			return pos;
		}

		public Vector3 GetRandomWorldPosInGridPos( GridPosition gridPos )
		{
			Vector2 pos = Get2DPosForGridPos( gridPos ) + new Vector2( Rand.Float( -SquareSize * 0.5f, SquareSize * 0.5f ), Rand.Float( -SquareSize * 0.5f, SquareSize * 0.5f ) );
			ImproveMovePosition( ref pos );
			return new Vector3( pos.x, pos.y, 0f );
		}

		public bool ImproveMovePosition( ref Vector2 movePos )
		{
			GridPosition gridPos = GetGridPosFor2DPos( movePos );
			Vector2 center = Get2DPosForGridPos( gridPos );

			// HighlightGridSquareB( gridPos, 1f );

			if ( AftermathGame.Instance.StructureManager.IsStructure( gridPos ) )
			{
				float left = center.x - SquareSize * 0.5f;
				float right = center.x + SquareSize * 0.5f;
				float down = center.y - SquareSize * 0.5f;
				float up = center.y + SquareSize * 0.5f;
			
				float minDist = float.MaxValue;
				bool unpenetrated = false;
				Vector2 originalPos = movePos;
			
				// left
				GridPosition leftGridPos = GetGridPosInDirection( gridPos, Direction.Left );
				if ( IsWalkable( leftGridPos ) )
				{
					float dist = originalPos.x - left;
					if ( dist < minDist )
					{
						minDist = dist;
						movePos = new Vector2( originalPos.x - dist - MOVE_POS_WALL_BUFFER, originalPos.y );
						unpenetrated = true;
					}
				}
			
				// right
				GridPosition rightGridPos = GetGridPosInDirection( gridPos, Direction.Right );
				if ( IsWalkable( rightGridPos ) )
				{
					float dist = right - originalPos.x;
					if ( dist < minDist )
					{
						minDist = dist;
						movePos = new Vector2( originalPos.x + dist + MOVE_POS_WALL_BUFFER, originalPos.y );
						unpenetrated = true;
					}
				}
			
				// down
				GridPosition downGridPos = GetGridPosInDirection( gridPos, Direction.Down );
				if ( IsWalkable( downGridPos ) )
				{
					float dist = originalPos.y - down;
					if ( dist < minDist )
					{
						minDist = dist;
						movePos = new Vector2( originalPos.x, originalPos.y - dist - MOVE_POS_WALL_BUFFER );
						unpenetrated = true;
					}
				}
			
				// up
				GridPosition upGridPos = GetGridPosInDirection( gridPos, Direction.Up );
				if ( IsWalkable( upGridPos ) )
				{
					float dist = up - originalPos.y;
					if ( dist < minDist )
					{
						minDist = dist;
						movePos = new Vector2( originalPos.x, originalPos.y + dist + MOVE_POS_WALL_BUFFER );
						unpenetrated = true;
					}
				}
			
				return unpenetrated;
			}
			else
			{
				float left = center.x - SquareSize * 0.5f + MOVE_POS_WALL_BUFFER;
				float right = center.x + SquareSize * 0.5f - MOVE_POS_WALL_BUFFER;
				float down = center.y - SquareSize * 0.5f + MOVE_POS_WALL_BUFFER;
				float up = center.y + SquareSize * 0.5f - MOVE_POS_WALL_BUFFER;

				// DebugOverlay.Line( new Vector3( center.x, center.y, 0.01f ), new Vector3( left, center.y, 0.01f ), (!IsWalkable( new GridPosition( gridPos.X - 1, gridPos.Y ) ) && movePos.x < left) ? Color.Red : Color.Black, 1f );
				// DebugOverlay.Line( new Vector3( center.x, center.y, 0.01f ), new Vector3( right, center.y, 0.01f ), Color.Black, 1f );
				// DebugOverlay.Line( new Vector3( center.x, center.y, 0.01f ), new Vector3( center.x, down, 0.01f ), Color.Black, 1f );
				// DebugOverlay.Line( new Vector3( center.x, center.y, 0.01f ), new Vector3( center.x, up, 0.01f ), Color.Black, 1f );
				//
				// DebugOverlay.Line( new Vector3( center.x, center.y, 0.01f ), new Vector3( movePos.x, movePos.y, 0.01f ), Color.Yellow, 1f );

				if ( !IsWalkable( new GridPosition( gridPos.X - 1, gridPos.Y ) ) && movePos.x < left )
					movePos = new Vector2( left, movePos.y );
				else if ( !IsWalkable( new GridPosition( gridPos.X + 1, gridPos.Y ) ) && movePos.x > right )
					movePos = new Vector2( right, movePos.y );
			
				if ( !IsWalkable( new GridPosition( gridPos.X, gridPos.Y - 1 ) ) && movePos.y < down )
					movePos = new Vector2( movePos.x, down );
				else if ( !IsWalkable( new GridPosition( gridPos.X, gridPos.Y + 1 ) ) && movePos.y > up )
					movePos = new Vector2( movePos.x, up );

				// DebugOverlay.Line( new Vector3( center.x, center.y, 0.01f ), new Vector3( movePos.x, movePos.y, 0.01f ), Color.Red, 1f );

				return true;
			}
		}

		public void HighlightGridSquare( GridPosition gridPos )
		{
			Vector2 pos = Get2DPosForGridPos( gridPos );
			Vector3 mins = new Vector3( pos.x - SquareSize * 0.5f, pos.y - SquareSize * 0.5f, 0.01f );
			Vector3 maxs = mins + new Vector3( SquareSize, SquareSize, SquareHeight );
			DebugOverlay.Box( 0f, mins, maxs, Color.Blue );
		}

		public void HighlightGridSquareB( GridPosition gridPos, float time = 1f )
		{
			Vector2 center2D = Get2DPosForGridPos( gridPos );
			Vector3 center = new Vector3( center2D.x, center2D.y, 0.01f );
			Vector3 XMinYMin = center + new Vector3( -SquareSize * 0.5f, -SquareSize * 0.5f, 0f );
			Vector3 XMinYMax = center + new Vector3( -SquareSize * 0.5f, SquareSize * 0.5f, 0f );
			Vector3 XMaxYMin = center + new Vector3( SquareSize * 0.5f, -SquareSize * 0.5f, 0f );
			Vector3 XMaxYMax = center + new Vector3( SquareSize * 0.5f, SquareSize * 0.5f, 0f );

			DebugOverlay.Line( XMinYMin, XMinYMax, Color.Black, time);
			DebugOverlay.Line( XMinYMax, XMaxYMax, Color.Black, time );
			DebugOverlay.Line( XMaxYMax, XMaxYMin, Color.Black, time );
			DebugOverlay.Line( XMaxYMin, XMinYMin, Color.Black, time );
		}
	}
}
