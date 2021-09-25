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
		}

		public override void Spawn()
		{
			base.Spawn();

			Hp = 10f;

			RotationSpeed = 5f;
			MeleeRotationSpeed = 2.5f;
			RotationController.RotationSpeed = RotationSpeed;

			// Gun gun = new Pistol();
			Gun gun = new Shotgun();
			GunHandler.StartEquippingGun( gun );
			GunHandler.FinishEquippingGun( gun );
		}

		public override void Assign( Player player )
		{
			base.Assign( player );
			RenderColor = player.TeamColor;

			Movement.MoveSpeed = 70f;
			Movement.FollowTargetMoveSpeed = 80f;

			CommandHandler.SetCommand( new LookForTargetCommand( CloseRangeDetectionDistance ) );
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
	}
}
