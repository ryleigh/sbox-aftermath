﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Entity = Sandbox.Entity;

namespace aftermath
{
	public partial class Zombie : AIPerson
	{
		private int _numWanders;
		private const float BASE_WANDER_TO_SURVIVOR_CHANCE = 0.23f;
		private const float FINAL_WANDER_TO_SURVIVOR_CHANCE = 0.80f;
		private const int FINAL_NUM_WANDERS = 15;

		private float _riseTimer;
		private float _riseDuration;
		private const float RISE_TIME_MIN = 1.2f;
		private const float RISE_TIME_MAX = 2.5f;
		private const float RISE_DEPTH = -90f;

		public override List<Person> GetValidTargets()
		{
			return Entity.All.OfType<Person>()
				.Where( person => !person.IsDead )
				.Where( person => person.PersonType != PersonType.Zombie)
				.ToList();
		}

		public Zombie()
		{
			PersonType = PersonType.Zombie;

			CloseRangeDetectionDistance = 65f;
			HearingRadius = 400f;
			_gridWanderDistance = 5;
		}

		public override void Spawn()
		{
			base.Spawn();

			Hp = 10f;

			RotationSpeed = 5f;
			MeleeRotationSpeed = 1.5f;
			MeleeAttackDelayTime = Rand.Float( 0.4f, 0.8f );
			MeleeAttackAttackTime = Rand.Float( 0.08f, 0.12f );
			MeleeAttackPauseTime = Rand.Float( 0.1f, 0.18f );
			MeleeAttackRecoverTime = Rand.Float( 0.12f, 0.16f );
			RotationController.RotationSpeed = RotationSpeed;
		}

		public override void Assign( Player player )
		{
			base.Assign( player );

			Log.Info( $"Zombie - Assign: {player}, color: {player?.TeamColor}" );

			Movement.MoveSpeed = 30f;
			Movement.FollowTargetMoveSpeed = 40f;

			RenderColor = new Color( Rand.Float( 0.2f, 0.25f ), Rand.Float( 0.5f, 0.7f ), Rand.Float( 0.2f, 0.25f ) );
			// Wander();
		}

		protected override void Tick()
		{
			base.Tick();

			float dt = Time.Delta;

			if ( IsSpawning )
			{
				_riseTimer += dt;

				if ( _riseTimer >= _riseDuration )
				{
					OnFinishSpawning();
				}
				else
				{
					Position = Position.WithZ( Utils.Map( _riseTimer, 0f, _riseDuration, RISE_DEPTH, 0f, EasingType.SineIn ) );
				}
			}
		}

		public override void FoundTarget( Person target )
		{
			base.FoundTarget( target );

			CommandHandler.SetCommand( new FollowTargetCommand( target ) );
		}

		public override void LostTarget( Person target, Vector2 lastSeenPos )
		{
			AftermathGame.Instance.SpawnFloater( Position, $"LostTarget {target.PersonName}!", new Color( 1f, 0f, 0.8f, 1f ) );

			MoveAndLook( lastSeenPos );
		}

		public override void PersonSpawn()
		{
			base.PersonSpawn();

			_riseTimer = 0f;
			_riseDuration = Rand.Float( RISE_TIME_MIN, RISE_TIME_MAX );

			Position = Position.WithZ( RISE_DEPTH );
		}

		public override void OnFinishSpawning()
		{
			base.OnFinishSpawning();

			Wander();
		}

		public override void Wander()
		{
			float wanderToSurvivorChance = Utils.Map( (float)_numWanders, 0f, (float)FINAL_NUM_WANDERS, BASE_WANDER_TO_SURVIVOR_CHANCE, FINAL_WANDER_TO_SURVIVOR_CHANCE, EasingType.Linear );
			if ( Rand.Float( 0f, 1f ) < wanderToSurvivorChance )
			{
				MoveToNearestSurvivor();
			}
			else
			{
				base.Wander();
			}

			_numWanders++;
		}

		public override void MeleeAttack( Vector2 dir, Person target )
		{
			AftermathGame.Instance.SpawnFloater( Position, $"Melee {target.PersonName}!", new Color( 1f, 0.1f, 0.1f, 1f ) );

			MeleeAttackCommand meleeAttackCommand = new MeleeAttackCommand( target )
			{
				Inaccuracy = MeleeAttackInaccuracy
			};

			meleeAttackCommand.PreAttackFinished += OnPreBiteFinished;

			CommandHandler.InsertCommand( meleeAttackCommand );
			_newWanderTimer = Rand.Float( NEW_WANDER_TIME_MIN, NEW_WANDER_TIME_MAX );
		}

		void OnPreBiteFinished( MeleeAttackCommand meleeAttackCommand )
		{
			// bite sparks
			// sfx
		}

		public void MoveToNearestSurvivor()
		{
			float closestDistSqr = float.MaxValue;
			Person closestSurvivor = null;

			List<Survivor> survivors = Entity.All.OfType<Survivor>().Where( survivor => !survivor.IsDead ).ToList();
			foreach ( Survivor survivor in survivors)
			{
				float sqrDist = (survivor.Position2D - Position2D).LengthSquared;
				if ( sqrDist < closestDistSqr )
				{
					closestDistSqr = sqrDist;
					closestSurvivor = survivor;
				}
			}

			if ( closestSurvivor != null )
			{
				MoveAndLook( closestSurvivor.Position2D );
			}
		}

		public override void CantPathToPos( Vector2 pos )
		{
			base.CantPathToPos( pos );

			Structure structure = GetNearbyStructure();
			if ( structure != null )
				Person.MoveToAttackStructure( structure, NetworkIdent );
		}
	}
}
