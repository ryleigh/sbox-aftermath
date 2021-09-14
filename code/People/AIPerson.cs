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
			float dt = Time.Delta;

			if ( IsSpawning ) return;

			HandleNewWandering(dt);

			if ( _investigationCooldownTimer > 0f )
				_investigationCooldownTimer -= dt;

			base.Tick();
		}

		protected override void OnFinishAllCommands( Person_CommandHandler commandHandler )
		{
			WaitCommand waitCommand = new WaitCommand( Rand.Float( 0.1f, 0.4f ) );
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
	}
}
