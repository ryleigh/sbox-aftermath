using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace aftermath
{
	public delegate void PersonMovementDelegate( Person_Movement personMovement );

	public class Person_Movement : PersonComponent
	{
		public float MoveSpeed { get; set; }
		public float FollowTargetMoveSpeed { get; set; }
		public float FootstepVolume { get; set; }

		public Vector2 Velocity { get; private set; }
		public float Deceleration { get; set; }

		public Vector2 ForceVelocity { get; private set; }
		public float ForceDeceleration { get; private set; }

		public float AllyRepelDistance { get; set; }
		public float AllyRepelStrength { get; set; }
		public float EnemyRepelDistance { get; set; }
		public float EnemyRepelStrength { get; set; }
		private float _repelTimer;
		private const float REPEL_TICK_MIN = 0.25f;
		private const float REPEL_TICK_MAX = 0.3f;

		public GridPosition CurrentGridPos { get; private set; }

		public event PersonMovementDelegate SwitchedGridPosition;

		public Person_Movement()
		{
			Deceleration = 0.8f;
			ForceDeceleration = 0.125f;
			_repelTimer = Rand.Float( REPEL_TICK_MIN, REPEL_TICK_MAX );

			AllyRepelDistance = 50f;
			AllyRepelStrength = 200f;
			EnemyRepelDistance = 50f;
			EnemyRepelStrength = 200f;

			// Log.Warning( $"Person_Movement ctor **************** IsServer: {Host.IsServer}," );
		}

		public override void Init()
		{
			CurrentGridPos = AftermathGame.Instance.GridManager.GetGridPosFor2DPos( Person.Position2D );
		}

		public override void Update( float dt )
		{
			// Log.Warning( $"Person_Movement Update 00000000000000000 IsServer: {Host.IsServer}, Person: {Person}" );

			if ( Person == null || Person.IsDead )
				return;

			RepelFromOtherPeople(dt);
			HandlePhysics(dt);
			CheckCurrentGridPos();
		}

		void HandlePhysics(float dt)
		{
			GridManager grid = AftermathGame.Instance.GridManager;
			Vector2 newPos = Person.Position2D + (Velocity + ForceVelocity) * dt;

			// check for wall collision (and for out-of-bounds)
			float BUFFER = Person.CollisionBounds.Size.x * 0.25f;
			float SQUARE_SIZE = grid.SquareSize;

			GridPosition gridPos = grid.GetGridPosFor2DPos( Person.Position2D );
			Vector2 center = grid.Get2DPosForGridPos( gridPos );
			float left = center.x - SQUARE_SIZE * 0.5f + BUFFER;
			float right = center.x + SQUARE_SIZE * 0.5f - BUFFER;
			float down = center.y - SQUARE_SIZE * 0.5f + BUFFER;
			float up = center.y + SQUARE_SIZE * 0.5f - BUFFER;

			if ( newPos.x < Person.Position2D.x ) {
				if ( newPos.x < left && !grid.IsWalkable( grid.GetGridPosInDirection( gridPos, Direction.Left ) ) ) {
					newPos = new Vector2( left, newPos.y );
				}
			} else if ( newPos.x > Person.Position2D.x ) {
				if ( newPos.x > right && !grid.IsWalkable( grid.GetGridPosInDirection( gridPos, Direction.Right ) ) ) {
					newPos = new Vector2( right, newPos.y );
				}
			}

			if ( newPos.y < Person.Position2D.y ) {
				if ( newPos.y < down && !grid.IsWalkable( grid.GetGridPosInDirection( gridPos, Direction.Down ) ) ) {
					newPos = new Vector2( newPos.x, down );
				}
			} else if ( newPos.y > Person.Position2D.y ) {
				if ( newPos.y > up && !grid.IsWalkable( grid.GetGridPosInDirection( gridPos, Direction.Up ) ) ) {
					newPos = new Vector2( newPos.x, up );
				}
			}

			var animHelper = new CitizenAnimationHelper( Person );
			
			animHelper.WithVelocity( Velocity );
			// animHelper.WithWishVelocity( dir );

			Person.SetPosition2D( newPos );

			if ( Host.IsServer)
				Velocity *= (1f - Deceleration);

			ForceVelocity *= (1f - ForceDeceleration);
		}

		public void AddVelocity( Vector2 amount )
		{
			if(Host.IsClient) return;

			Velocity += amount;
		}

		public void AddForceVelocity( Vector2 amount )
		{
			ForceVelocity += amount;
		}

		void RepelFromOtherPeople(float dt)
		{
			if ( Host.IsClient ) return;

			_repelTimer -= dt;
			if ( _repelTimer <= 0f )
			{
				bool repelFromAllies = Person.ShouldRepelFromAllies();

				var people = Entity.All.OfType<Person>().Where( person => !person.IsDead ).ToList();

				foreach ( var otherPerson in people )
				{
					if ( otherPerson != null && otherPerson != Person )
					{
						var isAlly = Person.PersonType == otherPerson.PersonType && Person.PlayerNum == otherPerson.PlayerNum;
						if ( isAlly && !repelFromAllies )
							break;

						float repelDistance = isAlly ? AllyRepelDistance : EnemyRepelDistance;
						float distSqr = (otherPerson.Position2D - Person.Position2D).LengthSquared;
						if ( distSqr < repelDistance * repelDistance )
						{
							// adjust pos if we are at exact same position
							if ( Person.Position2D.Equals( otherPerson.Position2D ) )
								Person.SetPosition2D( Person.Position2D + new Vector2( Rand.Float( -0.01f, 0.01f ), Rand.Float( -0.01f, 0.01f ) ) );

							float dist = (otherPerson.Position2D - Person.Position2D).Length;
							float repelStrength = Utils.Map( dist, repelDistance, 0f, 0f, isAlly ? AllyRepelStrength : EnemyRepelStrength, EasingType.QuadIn );
							AddForceVelocity( (Person.Position2D - otherPerson.Position2D).Normal * repelStrength );
						}
					}
				}

				_repelTimer = Rand.Float( REPEL_TICK_MIN, REPEL_TICK_MAX );
			}
		}

		public float GetCurrentMoveSpeed()
		{
			float speed = Person.CommandHandler.CurrentCommandType == PersonCommandType.FollowTarget ? FollowTargetMoveSpeed : MoveSpeed;

			// float speed = Utils.Map( Math.Abs( Person.BodyAnimHandler.AnimSin ), 0f, 1f, min, max, EasingType.Linear );

			// if ( Person.GunHandler.HasGun )
			// 	speed *= Person.GunHandler.Gun.MovementSpeedModifier;

			// return speed * Person.MoveSpeedFactor;

			return speed;
		}

		void CheckCurrentGridPos()
		{
			GridPosition lastGridPos = CurrentGridPos;
			CurrentGridPos = AftermathGame.Instance.GridManager.GetGridPosFor2DPos( Person.Position2D );
			if ( lastGridPos != CurrentGridPos )
			{
				GridPosChanged();
			}
		}

		void GridPosChanged()
		{
			SwitchedGridPosition?.Invoke( this );
		}
	}
}
