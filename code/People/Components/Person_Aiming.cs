using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public partial class Person_Aiming : PersonComponent
	{
		private Person_RotationController RotationController => Person.RotationController;

		public Vector2 BodyDirection => Utils.GetVector2FromAngleDegrees( Person.RotationController.CurrentRotation );
		public float TargetSightDegrees { get; protected set; }

		public float SightRadius { get; set; }
		public float SightAngle { get; set; }

		public bool IsInvestigating { get; private set; }
		public float InvestigationTargetDegrees { get; private set; }
		private float _investigationTimer;
		private float INVESTIGATION_TIME_MIN = 1.1f;
		private float INVESTIGATION_TIME_MAX = 3.0f;
		private float _investigationDelayTimer;
		private float INVESTIGATION_DELAY_TIME_MIN = 0.33f;
		private float INVESTIGATION_DELAY_TIME_MAX = 0.5f;
		private float _investigationCooldownTimer;
		private float INVESTIGATION_COOLDOWN_TIME_MIN = 0.66f;
		private float INVESTIGATION_COOLDOWN_TIME_MAX = 1.25f;

		public Person_Aiming( )
		{
			SightRadius = Rand.Float( 350f, 400f );
			SightAngle = Rand.Float( 115f, 130f );
		}

		public override void Update( float dt )
		{
			if ( Person.IsDead )
				return;

			if ( Person.IsSelected )
			{
				// DrawSightCone();
				//
				// Color color = (Person.CommandHandler.CurrentCommandType is PersonCommandType.AimAtTarget or PersonCommandType.Shoot)
				// 	? new Color( 1f, 0.5f, 0.5f, 0.45f )
				// 	: new Color( 0.6f, 0.6f, 1f, 0.3f );
				// Utils.DrawCircle( Person.Position.WithZ( 1f ), 1f, Person.CloseRangeDetectionDistance, 12, color, Time.Now * -2f );
				//
				// Utils.DrawCircle( Person.Position.WithZ( 1f ), 1f, Person.HearingRadius, 15, new Color( 0f, 0f, 0f, 0.25f ), Time.Now * -2f );

				// color = (Person.CommandHandler.CurrentCommandType is PersonCommandType.AimAtTarget or PersonCommandType.Shoot)
				// 	? new Color( 1f, 0f, 0f, 1f )
				// 	: new Color( 1f, 1f, 1f, 0.1f );
				// DebugOverlay.Line( Person.Position.WithZ( 1f ), Person.Position.WithZ( 1f ) + (Vector3)BodyDirection * SightRadius, color );
			}

			// DebugOverlay.Line( Person.EyePos, Person.EyePos + Person.EyeRot.Forward * 200f, Color.Orange);

			HandleAiming( dt );
		}

		void HandleAiming( float dt )
		{
			float targetDegrees = TargetSightDegrees;

			if ( IsInvestigating )
			{
				if ( _investigationDelayTimer <= 0f )
				{
					targetDegrees = InvestigationTargetDegrees;

					_investigationTimer -= dt;
					if ( _investigationTimer <= 0f )
					{
						IsInvestigating = false;
						_investigationCooldownTimer = Rand.Float( INVESTIGATION_COOLDOWN_TIME_MIN, INVESTIGATION_COOLDOWN_TIME_MAX );
					}
				}
				else
				{
					_investigationDelayTimer -= dt;
				}
			}

			RotationController.TargetRotation = targetDegrees;

			var animHelper = new CitizenAnimationHelper( Person );

			Vector2 dir = Utils.GetVector2FromAngleDegrees( targetDegrees );
			animHelper.WithLookAt( Person.EyePos + new Vector3( dir.x, dir.y, 0f ));
			// animHelper.WithWishVelocity( dir );

			if ( _investigationCooldownTimer > 0f )
				_investigationCooldownTimer -= dt;
		}

		void DrawSightCone()
		{
			List<Vector2> sightConePoints = new List<Vector2>();

			Vector2 forwardVector = BodyDirection * SightRadius;
			Vector2 leftVector = Utils.RotateVector2( forwardVector, SightAngle / 2f ) * SightRadius;
			Vector2 rightVector = Utils.RotateVector2( forwardVector, -SightAngle / 2f ) * SightRadius;
			float leftAngleRads = forwardVector.AngleRadians() + Utils.Deg2Rad( SightAngle / 2f );
			float rightAngleRads = forwardVector.AngleRadians() + Utils.Deg2Rad( -SightAngle / 2f );

			// left side of view cone
			Vector2 leftStart = Person.Position2D + leftVector * 1 / 256f;
			Vector2 leftEnd = Person.Position2D + leftVector;

			bool leftCollision = !HasDirectPath( leftStart, leftEnd, out var leftIntersectionPoint );

			float COLLISION_LENGTH = 0.92f;

			sightConePoints.Add( leftStart );
			sightConePoints.Add( leftCollision ? leftStart + (leftIntersectionPoint - leftStart) * COLLISION_LENGTH : leftEnd );

			// add in a blank so we don't connect the previous point with the next
			if ( leftCollision ) sightConePoints.Add( Vector2.Zero );

			int numSegments = (SightAngle / 8f).CeilToInt();
			float step = Math.Abs( rightAngleRads - leftAngleRads ) / (float)numSegments;

			for ( int i = 0; i < numSegments; i++ )
			{
				float toAngle;
				if ( i == numSegments - 1 ) toAngle = rightAngleRads;
				else toAngle = leftAngleRads - (i + 1) * step;

				Vector2 toPoint = new Vector2(
					Person.Position2D.x + SightRadius * MathF.Cos( toAngle ),
					Person.Position2D.y + SightRadius * MathF.Sin( toAngle )
				);

				if ( IsPosBlocked( toPoint ) || !HasDirectPath( Person.Position2D, toPoint, out _ ) )
					sightConePoints.Add( Vector2.Zero );
				else
					sightConePoints.Add( toPoint );
			}

			// right side of view cone
			Vector2 rightStart = Person.Position2D + rightVector * 1 / 256f;
			Vector2 rightEnd = Person.Position2D + rightVector;

			bool rightCollision = !HasDirectPath( rightStart, rightEnd, out var rightIntersectionPoint );

			if ( rightCollision ) sightConePoints.Add( Vector2.Zero );

			sightConePoints.Add( rightCollision ? rightStart + (rightIntersectionPoint - rightStart) * COLLISION_LENGTH : rightEnd );
			sightConePoints.Add( rightStart );

			Color color = (Person.CommandHandler.CurrentCommandType is PersonCommandType.AimAtTarget or PersonCommandType.Shoot)
				? new Color( 1f, 0.5f, 0.5f, 0.6f )
				: new Color( 0.6f, 0.6f, 1f, 0.4f );

			for ( int i = 0; i < sightConePoints.Count; i++ )
			{
				if ( i < sightConePoints.Count - 1 )
				{
					Vector2 from = sightConePoints[i];
					Vector2 to = sightConePoints[i + 1];

					if ( from.Equals( Vector2.Zero ) || to.Equals( Vector2.Zero ) )
						continue;

					DebugOverlay.Line( new Vector3( from.x, from.y, 1f ), new Vector3( to.x, to.y, 1f ), color );
				}
			}
		}

		public bool IsPosInsideSightCone( Vector2 pos )
		{
			if ( (pos - Person.Position2D).LengthSquared > MathF.Pow( SightRadius, 2f ) )
				return false;

			Vector2 forwardVector = BodyDirection * SightRadius;
			float leniency = (Person.PersonType == PersonType.Survivor) ? 5f : 0f;

			float leftAngle = forwardVector.AngleRadians() + Utils.Deg2Rad( SightAngle / 2f + leniency );
			float rightAngle = forwardVector.AngleRadians() - Utils.Deg2Rad( SightAngle / 2f + leniency );
			float angleToTarget = (pos - Person.Position2D).AngleRadians();

			return (angleToTarget > rightAngle && angleToTarget < leftAngle);
		}

		public void SetSightDirection( Vector2 direction )
		{
			RotationController.SetRotation( Utils.GetAngleDegreesFromVector( direction ) );
		}

		public void SetTargetSightDirection( Vector2 direction )
		{
			float degrees = Utils.GetAngleDegreesFromVector( direction );
			RotationController.TargetRotation = Utils.GetAngleDegreesFromVector( direction );
			TargetSightDegrees = degrees;
		}

		public bool HasDirectPath( Vector2 a, Vector2 b, out Vector2 intersectionPoint )
		{
			GridPosition gridPos;
			Vector2 normal;
			if ( !AftermathGame.Instance.GridManager.Raycast( a, b, RaycastMode.Sight, out gridPos, out intersectionPoint, out normal ) )
			{
				return true;
			}

			return false;
		}

		public bool CanSeePerson( Person target )
		{
			if ( IsPosInsideSightCone( target.Position2D ) )
			{
				GridPosition gridPos;
				Vector2 hitPos;
				Vector2 normal;
				if ( !AftermathGame.Instance.GridManager.Raycast( Person.Position2D, target.Position2D, RaycastMode.Sight, out gridPos, out hitPos, out normal ) )
				{
					return true;
				}
			}

			return false;
		}

		public bool IsPosBlocked( Vector2 pos )
		{
			return AftermathGame.Instance.GridManager.IsSightBlockingStructure( AftermathGame.Instance.GridManager.GetGridPosFor2DPos( pos ) );
		}

		public void Investigate( Vector2 pos )
		{
			if ( _investigationCooldownTimer > 0f )
				return;

			if ( IsPosInsideSightCone( pos ) )
				return;

			InvestigationTargetDegrees = Utils.GetAngleDegreesFromVector( pos - Person.Position2D );
			IsInvestigating = true;
			_investigationTimer = Rand.Float( INVESTIGATION_TIME_MIN, INVESTIGATION_TIME_MAX );

			// the closer we are, the quicker we'll respond
			float sqrDist = (pos - Person.Position2D).LengthSquared;
			float delayFactor = Utils.Map( sqrDist, 0f, MathF.Pow( Person.HearingRadius, 2f ), 0f, 1f, EasingType.SineOut);
			float delay = Rand.Float( INVESTIGATION_DELAY_TIME_MIN, INVESTIGATION_DELAY_TIME_MAX ) * delayFactor;
			_investigationDelayTimer = delay;

			// Person.Scaler.Scale( Rand.Float( 1.05f, 1.1f ), Rand.Float( 0.33f, 0.5f ) );

			AftermathGame.Instance.SpawnFloater( Person.Position, "HUH?", new Color( 1f, 1f, 1f, 0.5f ));
		}
	}
}
