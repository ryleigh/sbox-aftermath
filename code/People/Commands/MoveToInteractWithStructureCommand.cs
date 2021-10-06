using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aftermath
{
	public class MoveToInteractWithStructureCommand : PersonCommand
	{
		public Structure Structure { get; private set; }
		private const float REQ_DISTANCE = 70f;

		public override string ToString() { return $"MoveToInteract: {Structure?.GridPosition.ToString() ?? "NONE"}"; }

		public MoveToInteractWithStructureCommand( Structure structure )
		{
			Structure = structure;
			Type = PersonCommandType.MoveToInteractWithStructure;
		}

		public override void Begin()
		{
			base.Begin();

			if ( Structure == null || Structure.IsDestroyed || Person == null || Person.IsDead || !Structure.GetIsInteractable( Person ) )
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
				Structure.Interact( Person );
				Finish();
			}
		}
	}
}
