using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public enum StructureType { None, Wall }

	public partial class StructureManager : Entity
	{
		readonly SortedDictionary<int, Structure> _structures = new SortedDictionary<int, Structure>();
		readonly List<int> _emptyMiddleIndexes = new List<int>();
		readonly List<int> _emptyEdgeIndexes = new List<int>();

		public Structure GetStructure( int index ) { return _structures.ContainsKey( index ) ? _structures[index] : null; }
		public Structure GetStructure( GridPosition gridPos ) { return _structures.ContainsKey( AftermathGame.Instance.GridManager.GetIndexForGridPos( gridPos ) ) ? _structures[AftermathGame.Instance.GridManager.GetIndexForGridPos( gridPos )] : null; }
		public bool IsStructure( int index ) { return _structures.ContainsKey( index ); }
		public bool IsStructure( GridPosition gridPos ) { return _structures.ContainsKey( AftermathGame.Instance.GridManager.GetIndexForGridPos( gridPos ) ); }

		readonly List<int> _toRemove = new List<int>();

		public StructureManager()
		{
			Transmit = TransmitType.Always;

			// Log.Warning( $"StructureManager ctor **************** IsServer: {Host.IsServer}," );
		}

		public void Initialize()
		{
			// Log.Warning( $"StructureManager Initialize **************** IsServer: {Host.IsServer}," );

			GridManager grid = AftermathGame.Instance.GridManager;

			for ( int x = 0; x < grid.GridWidth; x++ )
			{
				for ( int z = 0; z < grid.GridDepth; z++ )
				{
					GridPosition gridPos = new GridPosition( x, z );
					int index = grid.GetIndexForGridPos( gridPos );

					if ( IsEdge( gridPos ) ) _emptyEdgeIndexes.Add( index );
					else _emptyMiddleIndexes.Add( index );
				}
			}
		}

		public void Update( float dt )
		{
			foreach ( KeyValuePair<int, Structure> pair in _structures )
			{
				Structure structure = pair.Value;

				if ( structure.IsUpdateable && !structure.IsDestroyed)
				{
					structure.Update( dt );
				}

				Color color = Color.Lerp( new Color( 1f, 1f, 1f, 0.1f ), new Color( 1f, 0.1f, 0.1f, 0.7f ), Utils.Map( structure.Hp, structure.MaxHp, 0f, 0f, 1f, EasingType.SineIn ) );
				DebugOverlay.Text( structure.Position, 0, $"{structure.Hp.FloorToInt()}/{structure.MaxHp}", color, 0f, float.MaxValue);
			}

			foreach ( int index in _toRemove )
			{
				if ( _structures.ContainsKey( index ) )
				{
					Structure structure = _structures[index];
					structure.Delete();
					_structures.Remove( index );
				}
			}
			_toRemove.Clear();
		}

		public Structure AddStructureServer( GridPosition gridPos, StructureType structureType, Direction structureDirection = Direction.None )
		{
			int index = AftermathGame.Instance.GridManager.GetIndexForGridPos( gridPos );
			if ( _structures.ContainsKey( index ) )
			{
				Log.Info($"AddStructure - structure already exists at {gridPos.ToString()}!");
				return null;
			}

			Structure structure = null;

			if ( structureType == StructureType.Wall )
			{
				structure = new Wall
				{
					StructureManager = this
				};
				structure.Tags.Add( "wall" );
			}

			if ( structure == null )
				return null;

			_structures.Add( index, structure );
			structure.SetGridPos( gridPos );
			structure.SetDirection( structureDirection );

			if ( IsEdge( gridPos ) ) _emptyEdgeIndexes.Remove( index );
			else _emptyMiddleIndexes.Remove( index );

			AddStructureClient( index, structure, gridPos );

			Log.Info( $"StructureManager - AddStructureServer: {structure} at index: {index} and gridPos: {gridPos}, blocksMovement: {structure.BlocksMovement}, IsServer: {IsServer}" );

			return structure;
		}

		[ClientRpc]
		public void AddStructureClient( int index, Structure structure, GridPosition gridPos)
		{
			structure.SetGridPos( gridPos );
			Log.Info( $"StructureManager - AddStructureClient: {structure} at index: {index} and gridPos: {structure.GridPosition}, blocksMovement: {structure.BlocksMovement}, IsServer: {IsServer}" );
			_structures.Add( index, structure );
		}

		public void RemoveStructure( Structure structure )
		{
			GridPosition gridPos = structure.GridPosition;

			int index = AftermathGame.Instance.GridManager.GetIndexForGridPos( gridPos );
			if ( !_structures.ContainsKey( index ) )
			{
				Log.Error( "RemoveStructure - structure does not exist at " + gridPos.ToString() + "!" );
				return;
			}

			_toRemove.Add( index );

			if ( IsEdge( gridPos ) ) _emptyEdgeIndexes.Add( index );
			else _emptyMiddleIndexes.Add( index );
		}

		public int GetRandomEmptyIndex()
		{
			if ( _emptyMiddleIndexes.Count == 0 )
				return -1;

			return _emptyMiddleIndexes[Rand.Int( 0, _emptyMiddleIndexes.Count )];
		}

		public int GetRandomEmptyEdgeIndex()
		{
			if ( _emptyEdgeIndexes.Count == 0 )
				return -1;

			return _emptyEdgeIndexes[Rand.Int( 0, _emptyEdgeIndexes.Count )];
		}

		public bool IsEdge( GridPosition gridPos ) { return (gridPos.X == 0 || gridPos.Y == 0 || gridPos.X == AftermathGame.Instance.GridManager.GridWidth - 1 || gridPos.Y == AftermathGame.Instance.GridManager.GridDepth - 1); }

		public Structure GetRandomStructure()
		{
			return _structures.Count == 0 ? null : _structures.ElementAt( Rand.Int( 0, _structures.Count ) ).Value;
		}
	}
}
