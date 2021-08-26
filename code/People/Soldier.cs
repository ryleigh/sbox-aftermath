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

			CloseRangeDetectionDistance = 10f;
			_gridWanderDistance = 13;
		}

		public override void Assign( Player player )
		{
			base.Assign( player );

			Log.Info( $"Soldier - Assign: {player}, color: {player?.TeamColor}" );

			Movement.MoveSpeed = 70f;
			Movement.FollowTargetMoveSpeed = 85f;

			RenderColor = new Color( Rand.Float( 0.7f, 0.8f ), Rand.Float( 0.7f, 0.8f ), Rand.Float( 0.1f, 0.15f ) );
			Wander();
		}
	}
}
