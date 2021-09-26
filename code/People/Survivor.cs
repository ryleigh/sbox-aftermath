using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Entity = Sandbox.Entity;

namespace aftermath
{
	public partial class Survivor : Person
	{
		public override List<Person> GetValidTargets()
		{
			return Entity.All.OfType<Person>()
				.Where( person => !person.IsDead )
				.Where( person => (person.PersonType == PersonType.Zombie || person.PersonType == PersonType.Soldier || person.PlayerNum != this.PlayerNum))
				.ToList();
		}

		public Survivor()
		{
			PersonType = PersonType.Survivor;

			CloseRangeDetectionDistance = 75f;
			HearingRadius = 375f;

			SpawnTimeMin = 0.6f;
			SpawnTimeMax = 0.7f;
		}

		public override void Spawn()
		{
			base.Spawn();

			Hp = 10f;

			RotationSpeed = 5f;
			MeleeRotationSpeed = 2.5f;
			RotationController.RotationSpeed = RotationSpeed;

			Gun gun = new Pistol();
			// Gun gun = new Shotgun();
			GunHandler.StartEquippingGun( gun );
			GunHandler.FinishEquippingGun( gun );
		}

		public override void Assign( Player player )
		{
			base.Assign( player );
			RenderColor = player.TeamColor;

			Movement.MoveSpeed = 70f;
			Movement.FollowTargetMoveSpeed = 80f;
		}

		protected override void OnFinishAllCommands( Person_CommandHandler commandHandler )
		{
			CommandHandler.SetCommand( new LookForTargetCommand( CloseRangeDetectionDistance ) );
		}

		public override void FoundTarget( Person target )
		{
			base.FoundTarget( target );

			CommandHandler.SetCommand( new AimAtTargetCommand( target ) );
		}

		public override void LostTarget( Person target, Vector2 lastSeenPos )
		{
			CommandHandler.SetCommand( new LookForTargetCommand( CloseRangeDetectionDistance ) );
		}

		public override void HeardNoise( Vector2 noisePos )
		{
			if ( CommandHandler.CurrentCommandType != PersonCommandType.LookForTarget )
				return;

			Aiming.Investigate( noisePos );
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
			CommandHandler.SetCommand( new LookForTargetCommand( CloseRangeDetectionDistance ) );
		}
	}
}
