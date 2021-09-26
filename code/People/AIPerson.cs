using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Rcon;
using Entity = Sandbox.Entity;

namespace aftermath
{
	public partial class AIPerson : Person
	{
		protected float _newWanderTimer;
		protected float NEW_WANDER_TIME_MIN = 3.5f;
		protected float NEW_WANDER_TIME_MAX = 4.5f;

		private float _investigationCooldownTimer;
		private float INVESTIGATION_COOLDOWN_TIME_MIN = 0.25f;
		private float INVESTIGATION_COOLDOWN_TIME_MAX = 0.66f;

		protected int _gridWanderDistance;

		public AIPerson()
		{
			IsAIControlled = true;
		}

		public override void Assign( Player player )
		{
			base.Assign( player );

			_newWanderTimer = Rand.Float( NEW_WANDER_TIME_MIN, NEW_WANDER_TIME_MAX );
		}

		protected override void Tick()
		{
			base.Tick();

			float dt = Time.Delta;

			if ( IsSpawning ) return;

			HandleNewWandering(dt);

			if ( _investigationCooldownTimer > 0f )
				_investigationCooldownTimer -= dt;
		}

		protected override void OnFinishAllCommands( Person_CommandHandler commandHandler )
		{
			float waitTime = Rand.Float( 0.1f, 0.4f );
			if ( Rand.Float( 0f, 1f ) < 0.2f )
				waitTime += Rand.Float( 0.25f, 3f );

			WaitCommand waitCommand = new WaitCommand(waitTime);

			waitCommand.WaitFinished += ( e ) => { Wander(); };
			CommandHandler.SetCommand( waitCommand );
		}

		private void HandleNewWandering(float dt)
		{
			PersonCommand command = CommandHandler.GetCurrentCommandIfExists( PersonCommandType.LookForTarget );
			if ( command != null )
			{
				_newWanderTimer -= dt;
				if(_newWanderTimer <= 0f)
					Wander();
			}
		}

		public virtual void Wander()
		{
			GridManager gridManager = AftermathGame.Instance.GridManager;
			int WIDTH = gridManager.GridWidth;
			int DEPTH = gridManager.GridDepth;

			// Log.Info( $"AIPerson - Wander: W: {WIDTH}, D: {DEPTH}, dist: {_gridWanderDistance}");

			int NUM_TRIES = 7;
			for ( int tries = 0; tries < NUM_TRIES; tries++ )
			{
				GridPosition gridPos = new GridPosition(
					Movement.CurrentGridPos.X + Rand.Int( -_gridWanderDistance, _gridWanderDistance),
					Movement.CurrentGridPos.Y + Rand.Int( -_gridWanderDistance, _gridWanderDistance )
				);

				if ( gridPos.X < 0 )
					gridPos = new GridPosition( gridPos.X * -1, gridPos.Y );
				else if ( gridPos.X > WIDTH - 1 )
					gridPos = new GridPosition( WIDTH - 1 - (gridPos.X - WIDTH - 1), gridPos.Y );

				if ( gridPos.Y < 0 )
					gridPos = new GridPosition( gridPos.X, gridPos.Y * -1 );
				else if ( gridPos.Y > DEPTH - 1 )
					gridPos = new GridPosition( gridPos.X, DEPTH - 1 - (gridPos.Y - DEPTH - 1) );

				// Log.Info( $"{tries}: curr: {Movement.CurrentGridPos } -> {gridPos}" );

				if ( !gridPos.Equals( Movement.CurrentGridPos ) && gridManager.IsWalkable( gridPos ) )
				{
					MoveAndLook( gridManager.Get2DPosForGridPos( gridPos ) );
					break;
				}
			}

			_newWanderTimer = Rand.Float( NEW_WANDER_TIME_MIN, NEW_WANDER_TIME_MAX );
		}

		public void MoveAndLook( Vector2 pos )
		{
			// Log.Info( $"AIPerson - MoveAndLook: {pos} {AftermathGame.Instance.GridManager.GetGridPosFor2DPos( pos )}" );
			MoveToPosCommand moveCommand = new MoveToPosCommand( pos );
			LookForTargetCommand lookCommand = new LookForTargetCommand( CloseRangeDetectionDistance );
			ParallelCommand parallelCommand = new ParallelCommand( new List<PersonCommand>() {moveCommand, lookCommand} )
			{
				Type = PersonCommandType.MoveAndLook
			};

			CommandHandler.SetCommand( parallelCommand );
		}

		protected Structure GetNearbyStructure()
		{
			Structure closestStructure = null;
			float closestDistSqr = float.MaxValue;

			int SEARCH_DEPTH = 3;

			GridManager gridManager = AftermathGame.Instance.GridManager;
			StructureManager structureManager = AftermathGame.Instance.StructureManager;

			for ( int x = -SEARCH_DEPTH; x <= SEARCH_DEPTH; x++ )
			{
				for ( int y = -SEARCH_DEPTH; y <= SEARCH_DEPTH; y++ )
				{
					GridPosition newGridPos = new GridPosition( Movement.CurrentGridPos.X + x, Movement.CurrentGridPos.Y + y );
					Structure structure = structureManager.GetStructure( newGridPos );
					if ( structure != null )
					{
						float sqrDist = (gridManager.Get2DPosForGridPos( newGridPos ) - Position2D).LengthSquared;
						if ( sqrDist < closestDistSqr )
						{
							closestDistSqr = sqrDist;
							closestStructure = structure;
						}
					}
				}
			}

			return closestStructure;
		}

		public override void HeardNoise( Vector2 noisePos )
		{
			if ( _investigationCooldownTimer > 0f )
				return;

			if ( CommandHandler.CurrentCommandType 
					is PersonCommandType.AimAtTarget 
					or PersonCommandType.Bite 
					or PersonCommandType.FollowTarget 
					or PersonCommandType.MoveToPickUpItem 
					or PersonCommandType.PickUpItem 
					or PersonCommandType.MoveToAttackStructure 
					or PersonCommandType.Reload 
					or PersonCommandType.Shoot )
				return;

			// if we're already moving towards a spot closer than the noisePos, ignore noise
			if ( CommandHandler.CurrentCommandType == PersonCommandType.MoveAndLook )
			{
				MoveToPosCommand moveToPosCommand = null;
				foreach ( PersonCommand personCommand in ((ParallelCommand)CommandHandler.CurrentCommand).SubCommands )
				{
					if ( personCommand.Type == PersonCommandType.MoveToPos )
					{
						moveToPosCommand = (MoveToPosCommand)personCommand;
						break;
					}
				}

				if ( moveToPosCommand != null )
				{
					float currMoveDistSqr = (moveToPosCommand.TargetPos - Position2D).LengthSquared;
					float newDistSqr = (noisePos - Position2D).LengthSquared;

					if ( currMoveDistSqr < newDistSqr )
						return;
				}
			}

			AftermathGame.Instance.SpawnFloater( Position, "?", Color.Black );

			MoveAndLook( noisePos );

			_investigationCooldownTimer = Rand.Float( INVESTIGATION_COOLDOWN_TIME_MIN, INVESTIGATION_COOLDOWN_TIME_MAX );
		}
	}
}
