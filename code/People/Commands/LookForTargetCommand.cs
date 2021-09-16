using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class LookForTargetCommand : PersonCommand
	{
		private float _lookTimer;
		public float LookDelayMin { get; set; }
		public float LookDelayMax { get; set; }

		public float CloseRangeDetectionDistance { get; set; }

		private const float CLOSE_PRIORITY = 35f; // the higher this is, the more closer targets are prioritized over directly-in-line-of-sight ones

		public override string ToString() { return "LookForTarget"; }

		public LookForTargetCommand( float closeRangeDetectionDistance )
		{
			Type = PersonCommandType.LookForTarget;

			LookDelayMin = 0.1f;
			LookDelayMax = 0.16f;
			_lookTimer = Rand.Float( LookDelayMin, LookDelayMax );

			CloseRangeDetectionDistance = closeRangeDetectionDistance;
		}

		public override void Update( float dt )
		{
			base.Update( dt );

			_lookTimer -= dt;
			if ( _lookTimer <= 0f )
			{
				ScanForTarget();
				_lookTimer = Rand.Float( LookDelayMin, LookDelayMax );
			}
		}

		private void ScanForTarget()
		{
			var targets = Person.GetValidTargets();
			Person chosenTarget = null;

			float bestNoticeability = float.MaxValue; // lower is more noticeable

			foreach ( var target in targets )
			{
				if(target == null || target.IsDead || target.IsSpawning || target == Person)
					continue;

				// check if we're close enough to disregard line of sight
				float distSqr = (target.Position2D - Person.Position2D).LengthSquared;

				// DebugOverlay.Line( Person.Position, target.Position, Color.Blue, 0.2f );
				// DebugOverlay.Text( Person.Position + (target.Position - Person.Position) * 0.5f, 0, (target.Position2D - Person.Position2D).Length.ToString(), Color.White, 0.2f, float.MaxValue);

				if ( distSqr < CloseRangeDetectionDistance * CloseRangeDetectionDistance )
				{
					// make sure they aren't obscured by a wall
					if ( AftermathGame.Instance.GridManager.Raycast( Person.Position2D, target.Position2D, RaycastMode.Sight, out _, out _, out _ ) )
						continue;
				}
				else
				{
					// if not close enough, check range/LOS/sight angle
					if(!Person.Aiming.CanSeePerson( target ))
						continue;
				}

				// we know we can see the target - but how noticeable are they compared to other targets?
				float angleDiff = Math.Abs( Utils.GetAngleDegreesFromVector( Person.Aiming.BodyDirection ) - Utils.GetAngleDegreesFromVector( target.Position2D - Person.Position2D ) );
				float noticeability = distSqr * CLOSE_PRIORITY + angleDiff;

				if ( noticeability < bestNoticeability )
				{
					bestNoticeability = noticeability;
					chosenTarget = target;
				}
			}

			if ( chosenTarget != null )
			{
				Person.FoundTarget( chosenTarget );
			}
		}
	}
}
