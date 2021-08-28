using System.Collections.Generic;
using Sandbox;
using System.Linq;

namespace aftermath
{
	public partial class Person_Pathfinding : PersonComponent
	{
		public List<Vector2> Path { get; private set; } = new List<Vector2>();
		private readonly List<GridPosition> _gridPath = new List<GridPosition>();

		public float TimeSinceLastPathfind { get; private set; }

		public bool SurvivorsBlock { get; set; }
		public bool ZombiesBlock { get; set; }
		public float SurvivorPathfindingAvoidanceAmount { get; set; }
		public float ZombiesPathfindingAvoidanceAmount { get; set; }

		private GridPosition _gridPosTreatAsWalkable;

		public Person_Pathfinding()
		{

		}

		public override void Update( float dt )
		{
			TimeSinceLastPathfind += dt;
		}

		public List<Vector2> GetPathTo( Vector2 a, Vector2 b, GridPosition treatAsWalkable = default( GridPosition ) )
		{
			GridManager grid = AftermathGame.Instance.GridManager;
			_gridPosTreatAsWalkable = treatAsWalkable;

			TimeSinceLastPathfind = 0f;

			Path.Clear();
			_gridPath.Clear();

			GridPosition gridPosA = grid.GetGridPosFor2DPos( a );
			GridPosition gridPosB = grid.GetGridPosFor2DPos( b );

			//            if (IsStructure(gridPosA) || IsStructure(gridPosB)) return new List<Vector>();
			if ( AftermathGame.Instance.StructureManager.IsStructure( gridPosB ) && !(treatAsWalkable.IsValid && treatAsWalkable == gridPosB) ) return Path;

			if ( grid.GetGridDistance( gridPosA, gridPosB ) <= 1 || CanPathDirectlyTo( a, b ) )
			{
				Path.Add( b );
				return Path;
			}

			bool foundPath = Utils.AStar<GridPosition>(
				gridPosA,
				gridPosB,
				_gridPath,
				GetEdges,
				GetHScoreFromGridPosToGridPos
			);

			if ( !foundPath )
				return Path;

			foreach ( GridPosition gridPos in _gridPath )
			{
				Path.Add( grid.Get2DPosForGridPos( gridPos ) );
			}

			Path = OptimizePathfinding( Path );

			// remove start pos
			Path.RemoveAt( 0 );

			return Path;
		}

		bool CanPathDirectlyTo( Vector2 start, Vector2 target )
		{
			GridPosition gridPos;
			Vector2 hitPos;
			Vector2 normal;

			// TODO: check each corner of model too
			bool direct = !AftermathGame.Instance.GridManager.Raycast( start, target, RaycastMode.Movement, out gridPos, out hitPos, out normal );

			return direct;
		}

		public List<Vector2> OptimizePathfinding( List<Vector2> path )
		{
			int i = 0;
			while ( i < path.Count - 2 )
			{
				if ( CanPathDirectlyTo( path[i], path[i + 2] ) )
					path.RemoveAt( i + 1 );
				else
					i++;
			}

			return path;
		}

		private readonly List<GridPosition> _walkable = new List<GridPosition>();

		public List<GridPosition> GetWalkableAdjacentGridPositions( GridPosition start )
		{
			_walkable.Clear();
			GridManager grid = AftermathGame.Instance.GridManager;

			GridPosition left = start + new GridPosition( -1, 0 );
			if ( grid.IsWalkable( left ) || (_gridPosTreatAsWalkable.IsValid && _gridPosTreatAsWalkable == left) )
			{
				_walkable.Add( left );
			}

			GridPosition right = start + new GridPosition( 1, 0 );
			if ( grid.IsWalkable( right ) || (_gridPosTreatAsWalkable.IsValid && _gridPosTreatAsWalkable == right) )
			{
				_walkable.Add( right );
			}

			GridPosition down = start + new GridPosition( 0, -1 );
			if ( grid.IsWalkable( down ) || (_gridPosTreatAsWalkable.IsValid && _gridPosTreatAsWalkable == down) )
			{
				_walkable.Add( down );
			}

			GridPosition up = start + new GridPosition( 0, 1 );
			if ( grid.IsWalkable( up ) || (_gridPosTreatAsWalkable.IsValid && _gridPosTreatAsWalkable == up) )
			{
				_walkable.Add( up );
			}

			return _walkable;
		}

		static float GetHScoreFromGridPosToGridPos( GridPosition a, GridPosition b )
		{
			return (b - a).ManhattanLength;
		}

		IEnumerable<AStarEdge<GridPosition>> GetEdges( GridPosition start )
		{
			var walkable = GetWalkableAdjacentGridPositions( start );
			return walkable.Select( gridPos => Utils.Edge( gridPos, GetCostToMoveFromGridPosToAdjacentGridPos( start, gridPos ) ) );
		}

		float GetCostToMoveFromGridPosToAdjacentGridPos( GridPosition a, GridPosition b )
		{
			float cost = 1f;
			if ( SurvivorsBlock && AftermathGame.Instance.GridManager.DoesGridPosContainSurvivor( b ) )
				cost += SurvivorPathfindingAvoidanceAmount;
			if ( ZombiesBlock && AftermathGame.Instance.GridManager.DoesGridPosContainZombie( b ) )
				cost += SurvivorPathfindingAvoidanceAmount;

			return cost;
		}
	}
}
