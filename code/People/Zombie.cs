using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Entity = Sandbox.Entity;

namespace aftermath
{
	public partial class Zombie : AIPerson
	{
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

			CloseRangeDetectionDistance = 8f;
			_gridWanderDistance = 5;
		}

		public override void Assign( Player player )
		{
			base.Assign( player );

			Log.Info( $"Zombie - Assign: {player}, color: {player?.TeamColor}" );

			Movement.MoveSpeed = 40f;
			Movement.FollowTargetMoveSpeed = 50f;

			RenderColor = new Color( Rand.Float( 0.2f, 0.25f ), Rand.Float( 0.5f, 0.7f ), Rand.Float( 0.2f, 0.25f ) );
			Wander();
		}
	}
}
