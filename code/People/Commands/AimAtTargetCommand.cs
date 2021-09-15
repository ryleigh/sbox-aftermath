using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class AimAtTargetCommand : PersonCommand
	{
		public Person Target { get; private set; }
		public bool HasTarget => (Target != null);

		public override string ToString() { return "AimAtTargetCommand: " + (Target != null ? Target.PersonName : "NONE"); }

		private float _loseTargetTimer;
		private const float LOSE_TARGET_TICK_MIN = 0.33f;
		private const float LOSE_TARGET_TICK_MAX = 0.66f;
		private int _numTimesCantSeeTarget;

		private const int NUM_TIMES_TO_LOSE_TARGET = 4;

		private float _evaluateShotTimer; // how often we should re-evaluate current shot to see if it's good enough to take
		private float EVALUATE_SHOT_TICK_MIN = 0.08f;
		private float EVALUATE_SHOT_TICK_MAX = 0.25f;

		private float _shootDelayTimer;

		public AimAtTargetCommand( Person target )
		{
			Target = target;

			Type = PersonCommandType.AimAtTarget;
		}

		public override void Begin()
		{
			base.Begin();

			if ( Person == null || Person.IsDead || Target == null || Target.IsDead )
			{
				Finish();
				return;
			}

			_loseTargetTimer = Rand.Float( LOSE_TARGET_TICK_MIN, LOSE_TARGET_TICK_MAX );
			_evaluateShotTimer = Rand.Float( EVALUATE_SHOT_TICK_MIN, EVALUATE_SHOT_TICK_MAX );

			if ( Person.GunHandler.HasGun )
				_shootDelayTimer = Rand.Float( Person.GunHandler.Gun.ShootDelayMin, Person.GunHandler.Gun.ShootDelayMax ) * Person.GunShootDelayFactor;

			// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.None );
		}

		public override void Update( float dt )
		{
			if(IsFinished)
				return;

			base.Update( dt );

			if(!Target.IsValid || Target == null || Target.IsDead || (!Person.GunHandler.HasGun && Person.PersonType == PersonType.Soldier))
			{
				Finish();
				return;
			}

			Person.Aiming.SetTargetSightDirection( Target.Position2D - Person.Position2D );

			// aim gun
			if ( Person.GunHandler.HasGun )
			{
				Gun gun = Person.GunHandler.Gun;
				gun.Rotation = Rotation.Slerp( gun.Rotation, Rotation.LookAt( Target.HeadPos, new Vector3( 0f, 0f, 1f )), dt * gun.AimSpeed * Person.GunAimSpeedFactor );

				// Person.DrawText( _evaluateShotTimer.ToString(), -3 );
				_evaluateShotTimer -= dt;
				_shootDelayTimer -= dt;

				if ( _evaluateShotTimer <= 0f && _shootDelayTimer <= 0f )
				{
					ConsiderShot();
					_evaluateShotTimer = Rand.Float( EVALUATE_SHOT_TICK_MIN, EVALUATE_SHOT_TICK_MAX );
				}
			}

			HandleLosingTarget( dt );
		}

		private void ConsiderShot()
		{
			Gun gun = Person.GunHandler.Gun;

			float rangeSqr = MathF.Pow( Rand.Float( gun.MinRange, gun.MaxRange ), 2f );
			if ( rangeSqr < (Target.Position2D - Person.Position2D).LengthSquared )
			{
				AftermathGame.Instance.SpawnFloater( Person.Position2D, $"TOO FAR!", new Color( 1f, 0.6f, 0.5f, 0.25f ) );
				return;
			}

			// float dot = gun.Rotation.Forward.Dot( (Target.HeadPos - gun.Position).Normal );
			float dot = Person.Rotation.Forward.Dot( (Target.HeadPos - Person.Position).Normal );

			if ( dot < gun.RequiredAimQuality )
			{
				AftermathGame.Instance.SpawnFloater( Person.Position2D, $"BAD ANGLE!", new Color( 1f, 0.3f, 0.5f, 0.6f ) );
				return;
			}

			Person.CommandHandler.InsertCommand( new ShootCommand(Target) );

			_shootDelayTimer = Rand.Float( gun.ShootDelayMin, gun.ShootDelayMax ) * Person.GunShootDelayFactor;
		}

		void HandleLosingTarget( float dt )
		{
			if ( HasTarget )
			{
				_loseTargetTimer -= dt;
				if ( _loseTargetTimer <= 0f )
				{
					bool canSeeTarget = Person.GunHandler.HasGun
						? Person.Aiming.CanSeePerson( Target ) && (Target.Position2D - Person.Position2D).LengthSquared < Person.GunHandler.Gun.MaxRange
						: Person.Aiming.CanSeePerson( Target );

					if ( canSeeTarget )
					{
						_numTimesCantSeeTarget = 0;
					}
					else
					{
						_numTimesCantSeeTarget++;
						if ( _numTimesCantSeeTarget >= NUM_TIMES_TO_LOSE_TARGET )
						{
							Finish();
						}
					}

					_loseTargetTimer = Rand.Float( LOSE_TARGET_TICK_MIN, LOSE_TARGET_TICK_MAX );
				}
			}
		}
	}
}
