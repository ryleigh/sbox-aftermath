using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class MoveToAttackStructureCommand : PersonCommand
	{
		public Structure Structure { get; private set; }
		private const float REQ_DISTANCE = 50f;

		public override string ToString() { return $"AttackStructure: {(Structure?.GridPosition.ToString() ?? "NONE")}"; }

		public MoveToAttackStructureCommand( Structure structure )
		{
			Structure = structure;
			Type = PersonCommandType.MoveToAttackStructure;
		}

		public override void Begin()
		{
			base.Begin();

			if ( Structure == null || Structure.IsDestroyed || Person == null || Person.IsDead )
			{
				Finish();
				return;
			}
		}

		public override void Update( float dt )
		{
			if ( IsFinished )
				return;

			base.Update( dt );

			if ( Structure == null || Structure.IsDestroyed || Person == null || Person.IsDead )
			{
				Finish();
				return;
			}

			CheckDistance( dt );
		}

		void CheckDistance( float dt )
		{
			float distSqr = (AftermathGame.Instance.GridManager.Get2DPosForGridPos( Structure.GridPosition ) - Person.Position2D).LengthSquared;
			if ( distSqr <= MathF.Pow( REQ_DISTANCE, 2f ) )
			{
				Person.CommandHandler.InsertCommand( new MeleeAttackCommand( Structure ) );
			}
		}
	}
}
