using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public partial class Structure : ModelEntity
	{
		public StructureManager StructureManager { get; set; }

		public StructureType StructureType { get; protected set; }
		public Direction FacingDirection { get; protected set; }

		public GridPosition GridPosition { get; private set; }
		public Vector2 Position2D { get; set; }

		public bool BlocksMovement { get; protected set; }
		public bool BlocksSight { get; protected set; }
		public bool IsUpdateable { get; protected set; }

		public Structure( )
		{
			Transmit = TransmitType.Always;
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();

			Log.Warning( $"Structure - ClientSpawn **************** IsServer: {Host.IsServer}, StructureManager: {AftermathGame.Instance.StructureManager}" );
		}

		public virtual void SetGridPos( GridPosition gridPos )
		{
			GridPosition = gridPos;
			Position2D = AftermathGame.Instance.GridManager.Get2DPosForGridPos( gridPos );

			if(IsServer)
				Position = AftermathGame.Instance.GridManager.GetWorldPosForGridPos( gridPos );
		}

		public virtual void SetDirection( Direction direction )
		{
			FacingDirection = direction;
		}
	}
}
