using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class MoveToBuildCommand : PersonCommand
	{
		public GridPosition GridPos { get; private set; }
		public Vector2 TargetPos { get; private set; }
		public StructureType StructureType { get; private set; }
		public Direction StructureDirection { get; private set; }

		private const float REQ_DISTANCE = 70f;
		public bool IsWaitingToBuild { get; private set; }

		private float _waitCheckTimer;
		private float WAIT_CHECK_DELAY = 0.1f;

		private int _cost;
		private bool _didStartBuilding;

		public override string ToString() { return $"MoveToBuild: {GridPos}, {StructureType}"; }

		public MoveToBuildCommand( GridPosition gridPos, StructureType structureType, Direction structureDirection, int cost )
		{
			StructureType = structureType;
			StructureDirection = structureDirection;
			GridPos = gridPos;

			_cost = cost;

			Type = PersonCommandType.MoveToBuild;
		}

		public override void Begin()
		{
			base.Begin();

			if ( !GridPos.IsValid || Person == null || Person.IsDead )
			{
				Finish();
				return;
			}

			TargetPos = AftermathGame.Instance.GridManager.Get2DPosForGridPos( GridPos );
		}

		public override void Update( float dt )
		{
			if(IsFinished)
				return;

			base.Update( dt );

			if ( Person == null || Person.IsDead )
			{
				Finish();
				return;
			}

			if ( IsWaitingToBuild )
			{
				_waitCheckTimer -= dt;
				if ( _waitCheckTimer <= 0f )
				{
					if ( !AftermathGame.Instance.GridManager.DoesGridPosContainPerson( GridPos ) )
					{
						StartBuilding();
					}
					else
					{
						AftermathGame.Instance.SpawnFloater( Person.Position2D, $"Build Blocked!", new Color( 0.4f, 0.6f, 1f, 1f ) );
					}

					_waitCheckTimer += WAIT_CHECK_DELAY;
				}
			}
			else
			{
				CheckDistance( dt );
			}
		}

		void CheckDistance( float dt )
		{
			float distSqr = (TargetPos - Person.Position2D).LengthSquared;
			if ( distSqr <= REQ_DISTANCE * REQ_DISTANCE )
			{
				if ( !AftermathGame.Instance.GridManager.DoesGridPosContainPerson( GridPos ) )
				{
					StartBuilding();
				}
				else
				{
					IsWaitingToBuild = true;
					_waitCheckTimer = WAIT_CHECK_DELAY;

					MoveToPosCommand moveCommand = Person.CommandHandler.GetCurrentCommandIfExists( PersonCommandType.MoveToPos ) as MoveToPosCommand;
					Log.Info( $"moveCommand: {moveCommand}" );
					if ( moveCommand != null )
						moveCommand.ShouldMove = false;
				}
			}
		}

		void StartBuilding()
		{
			_didStartBuilding = true;
			Person.CommandHandler.SetCommand( new BuildCommand( GridPos, StructureType, StructureDirection, _cost ) );
		}

		public override void Interrupt()
		{
			base.Interrupt();

			// Log.Info( "INTERRUPT ---------" );

			// if ( !_didStartBuilding )
			// 	Person.Player.AdjustScrapAmount( _cost );
		}
	}
}
