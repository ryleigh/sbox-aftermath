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

			CloseRangeDetectionDistance = 10f;
		}

		public override void Assign( Player player )
		{
			base.Assign( player );
			RenderColor = player.TeamColor;

			Movement.MoveSpeed = 70f;
			Movement.FollowTargetMoveSpeed = 80f;
		}
	}
}
