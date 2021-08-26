using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public partial class MoveToPosCommand : PersonCommand
	{
		public Vector2 TargetPos { get; private set; }
		public GridPosition TreatAsWalkable { get; private set; }
		public List<Vector2> Path;

		public override string ToString() { return $"MoveTo: {TargetPos}"; }

		private float _refreshPathTimer;
		public float RefreshDelayMin { get; set; } = 1f;
		public float RefreshDelayMax { get; set; } = 1.5f;

		protected float _walkingNoiseCooldown;
		protected float WALKING_NOISE_TICK_MIN = 0.33f;
		protected float WALKING_NOISE_TICK_MAX = 0.75f;

		public MoveToPosCommand( Vector2 pos, GridPosition treatAsWalkable = default( GridPosition ) )
		{
			TargetPos = pos;
			TreatAsWalkable = treatAsWalkable;

			Type = PersonCommandType.MoveToPos;

			// Log.Warning( $"MoveToPosCommand ctor **************** IsServer: {Host.IsServer}," );
		}

		public override void Begin()
		{
			base.Begin();

			if ( RefreshPath() )
			{
				// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.Move, Person.MoveAnimSpeed );
			}
			else
			{
				Finish();
			}
		}

		public override void Update( float dt )
		{
			if ( IsFinished )
				return;

			base.Update( dt );

			HandleMovement( dt );
			HandleWalkingSounds( dt );
			HandlePathRefreshing( dt );

			// if(Person.IsSelected) {
			//     DrawDebugLines();
			// }
		}

		void HandleWalkingSounds( float dt )
		{
			// _walkingNoiseCooldown -= dt;
			//
			// if ( _walkingNoiseCooldown <= 0f )
			// {
			// 	GameMode.NoiseManager.MakeNoise( Person.Position2D, Person.Movement.FootstepVolume, Person.PersonType );
			// 	_walkingNoiseCooldown = MathF.Random( WALKING_NOISE_TICK_MIN, WALKING_NOISE_TICK_MAX );
			// }
		}

		void HandleMovement( float dt )
		{
			// if ( IsFinished || Person == null || Person.IsDead || Path.Count == 0 )
			if ( IsFinished || Person == null || Path.Count == 0 )
			{
				Finish();
				return;
			}

			// DebugOverlay.ScreenText( 6, $"# Path: {Path.Count}, Length: {(Path[0] - Person.Position2D).Length}" );
			float distance = (Path[0] - Person.Position2D).Length;

			float REQ_DIST = 2f;
			if ( distance < REQ_DIST)
			{
				Person.SetPosition2D( Path[0] );
				Path.RemoveAt( 0 );

				if ( Path.Count == 0 )
				{
					Finish();
				}
				else
				{
					Person.Aiming.SetTargetSightDirection( Path[0] - Person.Position2D );
				}
			}
			else
			{
				Vector2 moveDir = (Path[0] - Person.Position2D).Normal;

				Person.Movement.AddVelocity( moveDir * Person.Movement.GetCurrentMoveSpeed() );

				// float speed = Path.Count > 1 ? 120f : (distance < 2f ? 0f : Utils.Map( distance, 0f, 50f, 0f, 120f, EasingType.ExpoOut ));
				// Vector2 velocity = moveDir * speed;
				// Person.Position2D += velocity * dt;
			}
		}

		void HandlePathRefreshing( float dt )
		{
			_refreshPathTimer -= dt;
			if ( _refreshPathTimer <= 0f )
			{
				RefreshPath();
			}
		}

		bool RefreshPath()
		{
			// if ( IsFinished || Person == null || Person.IsDead )
			if ( IsFinished || Person == null)
			{
				Finish();
				return false;
			}

			Path = Person.Pathfinding.GetPathTo( Person.Position2D, TargetPos, TreatAsWalkable );

			if ( Path.Count == 0 )
			{
				Finish();
				// Person.CantPathToPos( TargetPos );
				return false;
			}

			// client rpc
			Person.DrawPath( Path.ToArray() );

			Person.Aiming.SetTargetSightDirection( Path[0] - Person.Position2D );
			_refreshPathTimer = Rand.Float( RefreshDelayMin, RefreshDelayMax );

			return true;
		}

		public override void Finish()
		{
			if ( Host.IsServer )
				Person.Velocity = Vector3.Zero;
			// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.None );

			// if ( Person.IsExclusivelySelected )
			// 	Person.SelectingTool.MovementIndicator.HideMovementIndicator();

			// client rpc
			Person.StopDrawingPath();

			base.Finish();
		}

		public override void Interrupt()
		{
			base.Interrupt();

			if(Host.IsServer)
				Person.Velocity = Vector3.Zero;
			
			// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.None );
		}

		public override void Resume()
		{
			base.Resume();

			if ( Host.IsServer )
				Person.Velocity = Vector3.Zero;

			// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.Move, Person.MoveAnimSpeed );
		}
	}
}
