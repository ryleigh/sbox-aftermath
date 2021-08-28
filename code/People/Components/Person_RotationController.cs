using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public partial class Person_RotationController : PersonComponent
	{
		public float CurrentRotation { get; private set; }
		public float TargetRotation { get; set; }
		public float RotationSpeed { get; set; }

		public Person_RotationController()
		{
			// Log.Info( $"Person_RotationController ctor, IsServer: {Host.IsServer}," );
		}

		public override void Update( float dt )
		{
			if ( Math.Abs( CurrentRotation - TargetRotation ) < 1.0f )
				CurrentRotation = TargetRotation;
			
			if ( !CurrentRotation.Equals( TargetRotation ) )
			{
				if ( CurrentRotation > 0 && TargetRotation <= 0 )
				{
					if ( TargetRotation < (-180.0f + CurrentRotation) )
						TargetRotation = 360.0f + TargetRotation;
				}
				else if ( CurrentRotation <= 0 && TargetRotation > 0 )
				{
					if ( TargetRotation > (180.0f - Math.Abs( CurrentRotation )) )
						TargetRotation = -360.0f + TargetRotation;
				}
			
				// SetRotation( Utils.Lerp( CurrentRotation, TargetRotation, RotationSpeed * Person.RotationSpeedFactor * dt ) );
				SetRotation( Utils.Lerp( CurrentRotation, TargetRotation, RotationSpeed * dt ) );
			
				if ( CurrentRotation > 180.0f )
					CurrentRotation = -180.0f + (CurrentRotation - 180.0f);
				else if ( CurrentRotation < -180.0f )
					CurrentRotation = 180.0f - (CurrentRotation + 180.0f);
			}

			// Vector2 dir = Utils.GetVector2FromAngleDegrees( CurrentRotation );
			// DebugOverlay.Line( Person.Position.WithZ( 0.01f ), Person.Position.WithZ( 0.01f ) + new Vector3( dir.x, dir.y, 0f ) * 200f, Color.Yellow );
		}

		public void SetRotation( float degrees )
		{
			// Log.Info( $"SetRotation - degrees: {degrees}, CurrentRotation: {CurrentRotation}, TargetRotation: {TargetRotation}, RotationSpeed: {RotationSpeed}, IsServer: {Host.IsServer}," );

			CurrentRotation = degrees;

			if ( Host.IsServer )
			{
				Vector2 dir = Utils.GetVector2FromAngleDegrees( degrees );
				float degreesModified = Utils.GetAngleDegreesFromVector( new Vector2( dir.x, -dir.y ) );
				Person.Rotation = Rotation.From( new Angles( 0f, degreesModified, 0f ) );
			}
		}
	}
}
