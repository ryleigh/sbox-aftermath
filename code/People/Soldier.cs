using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;


namespace aftermath
{
	public partial class Soldier : AIPerson
	{
		public override List<Person> GetValidTargets()
		{
			return Entity.All.OfType<Person>()
				.Where( person => !person.IsDead )
				.Where( person => person.PersonType == PersonType.Zombie || person.PersonType == PersonType.Survivor )
				.ToList();
		}

		public Soldier()
		{
			PersonType = PersonType.Soldier;

			CloseRangeDetectionDistance = 75f;
			HearingRadius = 600f;
			_gridWanderDistance = 13;

			SpawnTimeMin = 3.4f;
			SpawnTimeMax = 3.5f;
		}

		public override void Spawn()
		{
			base.Spawn();

			Hp = 10f;

			RotationSpeed = 5f;
			MeleeRotationSpeed = 2.5f;
			MeleeAttackDelayTime = Rand.Float( 0.3f, 0.6f );
			MeleeAttackAttackTime = Rand.Float( 0.08f, 0.1f );
			MeleeAttackPauseTime = Rand.Float( 0.1f, 0.14f );
			MeleeAttackRecoverTime = Rand.Float( 0.1f, 0.14f );
			RotationController.RotationSpeed = RotationSpeed;

			// Gun gun = new Pistol();
			Gun gun = new Shotgun();
			GunHandler.StartEquippingGun( gun );
			GunHandler.FinishEquippingGun( gun );
		}

		public override void Assign( Player player )
		{
			base.Assign( player );

			Log.Info( $"Soldier - Assign: {player}, color: {player?.TeamColor}" );

			Movement.MoveSpeed = 70f;
			Movement.FollowTargetMoveSpeed = 85f;

			RenderColor = new Color( Rand.Float( 0.7f, 0.8f ), Rand.Float( 0.7f, 0.8f ), Rand.Float( 0.1f, 0.15f ) );
		}

		public override void FoundTarget( Person target )
		{
			base.FoundTarget( target );

			CommandHandler.SetCommand( new FollowTargetCommand( target ) );
		}

		public override void LostTarget( Person target, Vector2 lastSeenPos )
		{
			MoveAndLook( lastSeenPos );
		}

		public override void CantPathToPos( Vector2 pos )
		{
			base.CantPathToPos( pos );

			Structure structure = GetNearbyStructure();
			if ( structure != null )
				Person.MoveToAttackStructure( structure.NetworkIdent, NetworkIdent );
		}

		public override void MeleeAttack( Vector2 dir, Person target )
		{
			AftermathGame.Instance.SpawnFloater( Position, $"Melee {target.PersonName}!", new Color( 1f, 0.1f, 0.3f, 1f ) );

			MeleeAttackCommand meleeAttackCommand = new MeleeAttackCommand( target )
			{
				Inaccuracy = MeleeAttackInaccuracy
			};

			meleeAttackCommand.PreAttackFinished += OnPreAttackFinished;
			meleeAttackCommand.PauseFinished += OnPauseFinished;

			// WieldKnife();

			CommandHandler.InsertCommand( meleeAttackCommand );
			_newWanderTimer = Rand.Float( NEW_WANDER_TIME_MIN, NEW_WANDER_TIME_MAX );
		}

		void OnPreAttackFinished( MeleeAttackCommand meleeAttackCommand )
		{
			// sfx
		}

		void OnPauseFinished( MeleeAttackCommand meleeAttackCommand )
		{
			// HolsterKnife();
		}

		public override void PersonSpawn()
		{
			base.PersonSpawn();

			Position = Position.WithZ( FALL_HEIGHT );
		}

		protected override void UpdateSpawning( float dt )
		{
			base.UpdateSpawning( dt );

			Position = Position.WithZ( Utils.Map( SpawnTimer, 0f, SpawnDuration, FALL_HEIGHT, 0f, EasingType.SineIn ) );
		}

		public override void FinishSpawning()
		{
			base.FinishSpawning();

			Position = Position.WithZ( 0f );
			Wander();
		}
	}
}
