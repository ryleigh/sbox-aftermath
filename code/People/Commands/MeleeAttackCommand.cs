using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

namespace aftermath
{
	enum AttackMode { PreAttack, Attack, Pause, Recover }

	public delegate void MeleeAttackDelegate( MeleeAttackCommand meleeAttackCommand );

	public class MeleeAttackCommand : PersonCommand
	{
		public Person TargetPerson { get; private set; }

		public Structure TargetStructure { get; private set; }
		public Vector2 TargetStructurePos { get; private set; }

		public float Inaccuracy { get; set; }

		private float _timer;

		private AttackMode _mode = AttackMode.PreAttack;

		private float _startAngle;
		private float _endAngle;
		private float _force;

		public event MeleeAttackDelegate PreAttackFinished;
		public event MeleeAttackDelegate PauseFinished;

		public MeleeAttackCommand( Person targetPerson )
		{
			TargetPerson = targetPerson;

			InitValues();
		}

		public MeleeAttackCommand( Structure structure )
		{
			TargetStructure = structure;

			InitValues();
		}

		void InitValues()
		{
			Type = PersonCommandType.Bite;

			_startAngle = Rand.Float( 10f, 25f );
			_endAngle = Rand.Float( 10f, 25f );
			_force = Rand.Float( 0.7f, 2f );
		}

		public override void Begin()
		{
			base.Begin();

			if ( TargetStructure != null )
				TargetStructurePos = AftermathGame.Instance.GridManager.Get2DPosForGridPos( TargetStructure.GridPosition );

			Person.RotationController.RotationSpeed = Person.MeleeRotationSpeed;
		}

		public override void Update( float dt )
		{
			if( IsFinished ) return;

			base.Update( dt );

			Vector2 targetPos = TargetPerson?.Position2D ?? TargetStructurePos;
			Person.Aiming.SetTargetSightDirection( targetPos - Person.Position2D );

			_timer += dt;

			switch ( _mode )
			{
				case AttackMode.PreAttack:
					if ( _timer >= Person.MeleeAttackDelayTime )
					{
						Vector2 direction = Utils.RotateVector2( Person.Aiming.BodyDirection, Rand.Float( -Inaccuracy, Inaccuracy ) );

						Person.Movement.AddForceVelocity( direction * _force );
						_mode = AttackMode.Attack;
						_timer = 0f;

						PreAttackFinished?.Invoke( this );
					}
					else
					{
						float progress = Utils.Map( _timer, 0f, Person.MeleeAttackDelayTime, 0f, 1f, EasingType.SineOut );
						// rotate body backwards
					}
					
					break;
				case AttackMode.Attack:
					if ( _timer >= Person.MeleeAttackAttackTime )
					{
						_mode = AttackMode.Pause;
						_timer = 0f;

						CheckForContact();
					}
					else
					{
						float progress = Utils.Map( _timer, 0f, Person.MeleeAttackAttackTime, 0f, 1f, EasingType.SineOut );
						// rotate body forward
					}
					break;
				case AttackMode.Pause:
					if ( _timer >= Person.MeleeAttackPauseTime )
					{
						_mode = AttackMode.Recover;
						_timer = 0f;

						PauseFinished?.Invoke( this );
					}
					break;
				case AttackMode.Recover:
					if ( _timer >= Person.MeleeAttackPauseTime )
					{
						Finish();
					}
					else
					{
						float progress = Utils.Map( _timer, 0f, Person.MeleeAttackRecoverTime, 0f, 1f, EasingType.SineOut );
						// reset body rotation
					}
					break;
			}
		}

		void CheckForContact()
		{
			float REQ_DIST_PERSON = 50f;
			float REQ_DIST_STRUCTURE = 60f;

			Person hitPerson = null;
			float closestDistSqr = float.MaxValue;

			// check if we hit
			foreach ( Person target in Person.GetValidTargets() )
			{
				float distSqr = (target.HeadPos - Person.HeadPos).LengthSquared;
				if ( distSqr < closestDistSqr && distSqr < REQ_DIST_PERSON * REQ_DIST_PERSON )
				{
					hitPerson = target;
					closestDistSqr = distSqr;
				}
			}

			if ( hitPerson != null )
			{
				hitPerson.HitByMelee( Person, Person.HeadPos );
			}
			else
			{
				GridManager grid = AftermathGame.Instance.GridManager;
				// if we don't hit a person, check if we hit a structure
				GridPosition facingGridPos = grid.GetGridPosFor2DPos( Person.Position2D + Person.Aiming.BodyDirection * grid.SquareSize );
				if(!facingGridPos.IsValid || !grid.IsGridPosInBounds( facingGridPos ))
					return;

				Structure structure = AftermathGame.Instance.StructureManager.GetStructure( facingGridPos );
				if ( structure != null )
				{
					float distSqr = (grid.Get2DPosForGridPos( facingGridPos ) - Person.Position2D).LengthSquared;
					if ( distSqr < REQ_DIST_STRUCTURE * REQ_DIST_STRUCTURE )
					{
						structure.MeleeAttacked( Person, Person.HeadPos );
					}
				}
			}
		}

		public override void Finish()
		{
			Person.RotationController.RotationSpeed = Person.RotationSpeed;

			base.Finish();
		}

		public override void Interrupt()
		{
			base.Interrupt();

			Person.RotationController.RotationSpeed = Person.RotationSpeed;
		}

		public override void Resume()
		{
			base.Resume();

			Person.RotationController.RotationSpeed = Person.MeleeRotationSpeed;
		}
	}
}
