
namespace aftermath
{
	public class FrustumSelect
	{
		Rotation Rotation;
		Ray StartRay;
		Ray EndRay;

		internal void Init( Ray ray, Rotation rotation )
		{
			StartRay = ray;
			Rotation = rotation;
			IsDragging = false;
		}

		internal void Update( Ray ray )
		{
			EndRay = ray;

			IsDragging = Vector3.DistanceBetween( StartRay.Project( 100 ), EndRay.Project( 100 ) ) > 5.0f;
		}

		public bool IsDragging { get; internal set; }

		internal Frustum GetFrustum( float znear = 0, float zfar = float.MaxValue )
		{
			var left = Rotation.Left;
			var up = Rotation.Up;

			var rayA = StartRay.Project( 100 );
			var rayB = EndRay.Project( 100 );

			Frustum f = new Frustum();

			var forward = (StartRay.Direction + EndRay.Direction).Normal;

			if ( left.Dot( (rayA - rayB).Normal ) < 0 )
			{
				f.LeftPlane = new Plane( EndRay.Origin, EndRay.Direction.Cross( up ) );
				f.RightPlane = new Plane( StartRay.Origin, StartRay.Direction.Cross( -up ) );
			}
			else
			{
				f.LeftPlane = new Plane( StartRay.Origin, StartRay.Direction.Cross( up ) );
				f.RightPlane = new Plane( EndRay.Origin, EndRay.Direction.Cross( -up ) );
			}

			if ( up.Dot( (rayA - rayB).Normal ) < 0 )
			{
				f.TopPlane = new Plane( EndRay.Origin, EndRay.Direction.Cross( -left ) );
				f.BottomPlane = new Plane( StartRay.Origin, StartRay.Direction.Cross( left ) );
			}
			else
			{
				f.TopPlane = new Plane( StartRay.Origin, StartRay.Direction.Cross( -left ) );
				f.BottomPlane = new Plane( EndRay.Origin, EndRay.Direction.Cross( left ) );
			}

			f.NearPlane = new Plane( (StartRay.Origin + forward * znear), forward );
			f.FarPlane = new Plane( (StartRay.Origin + forward * zfar), -forward );

			return f;
		}
	}
}
