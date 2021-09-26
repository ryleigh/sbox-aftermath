using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class FollowTargetCommand : PersonCommand
	{
		public Person Target { get; private set; }
		public bool HasTarget => (Target != null);
		public Vector2 LastSeenTargetPos { get; private set; }

		public List<Vector2> Path;

		public override string ToString() { return $"Follow: {(Target != null ? Target.PersonName : "NONE")}"; }

		private float _loseTargetTimer;
		private const float LOSE_TARGET_TICK_MIN = 0.33f;
		private const float LOSE_TARGET_TICK_MAX = 0.66f;
		private int _numTimesCantSeeTarget;
		private const int NUM_TIMES_TO_LOSE_TARGET = 4;

		private float _refreshPathTimer;
		public float RefreshDelayMin { get; set; } = 1f;
		public float RefreshDelayMax { get; set; } = 1.5f;

		private float _evaluateShotTimer;
		private const float EVALUATE_SHOT_DELAY_MIN = 0.06f;
		private const float EVALUATE_SHOT_DELAY_MAX = 0.21f;

		protected float _walkingNoiseCooldown;
		protected float WALKING_NOISE_TICK_MIN = 0.33f;
		protected float WALKING_NOISE_TICK_MAX = 0.75f;

		public FollowTargetCommand( Person target )
		{
			Target = target;

			Type = PersonCommandType.FollowTarget;
		}

		public override void Begin()
		{
			base.Begin();

			if ( Target == null || Target.IsDead )
			{
				Finish();
				return;
			}

			RefreshPath();
			Target.DiedCallback += OnTargetDied;

			_loseTargetTimer = Rand.Float( LOSE_TARGET_TICK_MIN, LOSE_TARGET_TICK_MAX );
		}

		public override void Update( float dt )
		{
			if ( IsFinished ) return;

			base.Update( dt );

			HandleMovement( dt );
			HandleWalkingSounds( dt );
			HandleAttacking( dt );
			HandleLosingTarget( dt );
			HandlePathRefreshing( dt );
		}

		void HandleMovement( float dt )
		{
			if ( IsFinished || Person == null || Path.Count == 0 )
			{
				Finish();
				return;
			}

			float distance = (Path[0] - Person.Position2D).Length;

			float REQ_DIST = 2f;
			if ( distance < REQ_DIST )
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
			}
		}

		void HandleWalkingSounds( float dt )
		{
			_walkingNoiseCooldown -= dt;

			if ( _walkingNoiseCooldown <= 0f )
			{
				AftermathGame.Instance.MakeNoise( Person.Position2D, loudness: Person.Movement.FootstepVolume, noiseType: Person.PersonType );
				_walkingNoiseCooldown = Rand.Float( WALKING_NOISE_TICK_MIN, WALKING_NOISE_TICK_MAX );
			}
		}

		void HandleAttacking( float dt )
		{
			if ( Target == null )
				return;

			if ( (Target.Position2D - Person.Position2D).LengthSquared < MathF.Pow( Rand.Float( Person.MeleeRangeMin, Person.MeleeRangeMax ), 2f ) )
			{
				if ( Person.GunHandler.HasGun )
				{
					Person.GunHandler.DropGun(
						new Vector2( Rand.Float( -1f, 1f ), Rand.Float( -1f, 1f ) ).Normal,
						Rand.Float( Person_GunHandler.DROP_FORCE_MIN, Person_GunHandler.DROP_FORCE_MAX ),
						Rand.Float( 4f, 8f ),
						Rand.Int( Person_GunHandler.DROP_NUM_FLIPS_MIN, Person_GunHandler.DROP_NUM_FLIPS_MAX )
					);
				}

				Person.MeleeAttack( Person.Aiming.BodyDirection, Target );
			}
			else if ( Person.GunHandler.HasGun )
			{
				PrepareToShoot( dt );
			}
		}

		void PrepareToShoot( float dt )
		{
			_evaluateShotTimer -= dt;
			if ( _evaluateShotTimer <= 0f )
			{
				ConsiderShot();
				_evaluateShotTimer = Rand.Float( EVALUATE_SHOT_DELAY_MIN, EVALUATE_SHOT_DELAY_MAX );
			}
		}

		void ConsiderShot()
		{
			// AftermathGame.Instance.SpawnFloater( Person.Position, $"CONSIDER SHOT!", new Color( 0f, 1f, 0.3f, 1f ) );

			Gun gun = Person.GunHandler.Gun;

			if ( (Target.Position2D - Person.Position2D).Length > Rand.Float( gun.MinRange, gun.MaxRange ) )
				return;

			Person.CommandHandler.InsertCommand( new AimAtTargetCommand( Target ) );
		}

		void HandleLosingTarget( float dt )
		{
			if ( HasTarget )
			{
				// DebugOverlay.Line( Person.Position, Target.Position, Person.Aiming.CanSeePerson( Target ) ? Color.Red : Color.Magenta );

				_loseTargetTimer -= dt;
				if ( _loseTargetTimer <= 0f )
				{
					if ( Person.Aiming.CanSeePerson( Target ) )
					{
						_numTimesCantSeeTarget = 0;
						LastSeenTargetPos = Target.Position2D;
					}
					else
					{
						_numTimesCantSeeTarget++;
						if ( _numTimesCantSeeTarget >= NUM_TIMES_TO_LOSE_TARGET )
						{
							Person.LostTarget( Target, LastSeenTargetPos );
						}
					}

					_loseTargetTimer = Rand.Float( LOSE_TARGET_TICK_MIN, LOSE_TARGET_TICK_MAX );
				}
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

		void RefreshPath()
		{
			if ( IsFinished || Person == null )
			{
				Finish();
				return;
			}

			Path = Person.Pathfinding.GetPathTo( Person.Position2D, Target.Position2D );

			if ( Path.Count == 0 )
			{
				Finish();
				return;
			}

			Person.DrawPath( Path.ToArray() );

			Person.Aiming.SetTargetSightDirection( Path[0] - Person.Position2D );
			_refreshPathTimer = Rand.Float( RefreshDelayMin, RefreshDelayMax );
		}

		public override void Finish()
		{
			Target.DiedCallback -= OnTargetDied;

			base.Finish();
		}

		public override void Interrupt()
		{
			base.Interrupt();

			Log.Warning( "FOLLOW INTERRUPT!" );

			Target.DiedCallback -= OnTargetDied;
		}

		public override void Resume()
		{
			if ( Target.IsDead )
			{
				Finish();
				return;
			}

			// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.Move, Person.FollowTargetMoveAnimSpeed );
		}

		void OnTargetDied( Person person )
		{
			if ( !IsPaused )
				Finish();
		}
	}
}
