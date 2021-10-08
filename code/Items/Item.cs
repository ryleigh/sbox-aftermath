using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public delegate void ItemDelegate( Item item );

	public partial class Item : ModelEntity
	{
		[Net] public string DebugText { get; set; }

		void DrawDebugText()
		{
			if ( string.IsNullOrEmpty( DebugText ) ) return;

			Color color = new Color( 0.66f, 0.66f, 0.66f, 0.3f );
			float duration = 0f;
			float dist = float.MaxValue;

			DebugOverlay.Text( Position, 2, DebugText, color, duration, dist );
		}

		[Net] public Vector2 Position2D { get; private set; }

		public void SetPosition2D( Vector2 pos )
		{
			if ( IsServer )
			{
				Position2D = pos;
				Position = new Vector3( pos.x, pos.y, Position.z );
			}
		}

		[Net] public bool MovementActive { get; set; }

		public Vector2 Velocity2D { get; set; }

		[Net] public bool IsInAir { get; protected set; }

		protected float _deceleration;
		protected float _groundedDeceleration;

		protected float _airTimer;
		protected float _airTimeTotal;
		protected float _peakHeight;
		protected float _startingHeight;
		protected float _startingRotation;
		protected float _targetRotation;
		protected float _groundHeight;

		private Vector2 _horizontalTargetPos;
		private Vector2 _horizontalStartingPos;
		private bool _useHorizontalTargetPos;

		public bool IsBeingPickedUp => PersonPickingUp != null || StructurePickingUp != null;

		[Net] public Person PersonPickingUp { get; set; }
		[Net] public Structure StructurePickingUp { get; set; }
		[Net] public bool IsBeingHovered { get; protected set; }
		[Net] public int NumPeopleMovingToPickUp { get; set; }

		protected bool _disablePhysicsOnRest;

		public event ItemDelegate HitGroundCallback;

		protected float _lifetime;
		private float _blinkTimer;
		private readonly float BLINK_START_TIME = 5f;
		protected bool _shouldBlink = true;
		[Net] public bool IsVisible { get; private set; }

		public string ModelPath { get; protected set; } = "models/citizen_props/roadcone01.vmdl";

		public Item()
		{
			Transmit = TransmitType.Always;

			_deceleration = 0.05f;
			_groundedDeceleration = 0.2f;
			MovementActive = true;
			_disablePhysicsOnRest = true;
			IsVisible = true;

			_lifetime = float.MaxValue;
		}

		public override void Spawn()
		{
			// SetModel( "models/citizen_props/balloonregular01.vmdl" );
			SetModel( ModelPath );

			// CollisionGroup = CollisionGroup.Player;
			// SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			// SetupPhysicsFromCapsule( PhysicsMotionType.Dynamic, Capsule.FromHeightAndRadius( 64f, 26f ) ); // 8 radius default
			SetupPhysicsFromModel( PhysicsMotionType.Static );
			EnableHitboxes = true;

			// EnableSolidCollisions = false;
			// EnableTraceAndQueries = true;
		}

		[Event.Tick.Server]
		protected virtual void Tick()
		{
			float dt = Time.Delta;

			if ( MovementActive )
			{
				HandleHorizontalPhysics( dt );
				HandleVerticalPhysics( dt );

				if ( _disablePhysicsOnRest && !IsInAir && Velocity2D.Length < 0.1f )
				{
					Velocity2D = Vector2.Zero;
					MovementActive = false;
				}
			}

			if ( AllowedToDespawn() )
				HandleLifetime( dt );
			else if ( !IsVisible )
				SetVisible( true );

			// DebugText = $"Phys: {PhysicsActive}";
		}

		[Event.Tick.Client]
		protected virtual void ClientTick()
		{
			float dt = Time.Delta;
			DrawDebugText();

			// DebugOverlay.Text( Position, -2, $"{_useHorizontalTargetPos }", Color.Green, 0f, float.MaxValue );
		}

		private void HandleLifetime( float dt )
		{
			_lifetime -= dt;

			if ( _lifetime < 0f )
				LifetimeFinished();
			else if ( _lifetime < BLINK_START_TIME )
				HandleBlinking( dt );
		}

		protected virtual void LifetimeFinished()
		{
			Delete();
		}

		private void HandleBlinking( float dt )
		{
			if ( !_shouldBlink ) return;

			_blinkTimer -= dt;
			if ( _blinkTimer <= 0f )
			{
				SetVisible( !IsVisible );
				_blinkTimer = Utils.Map( _lifetime, BLINK_START_TIME, 0f, 0.1f, 0.01f, EasingType.CubicIn );
			}
		}

		public void SetVisible( bool visible )
		{
			if ( IsVisible == visible ) return;

			RenderColor = RenderColor.WithAlpha( visible ? 1f : 0f );
			IsVisible = visible;
		}

		protected virtual bool AllowedToDespawn()
		{
			return true;
		}

		private void HandleHorizontalPhysics( float dt )
		{
			if ( _useHorizontalTargetPos )
				HandleHorizontalTargetMovement( dt );
			else
				HandleHorizontalVelocity( dt );
		}

		private void HandleHorizontalVelocity( float dt )
		{
			if ( Velocity2D.LengthSquared > 0f )
			{
				GridManager grid = AftermathGame.Instance.GridManager;
				Vector2 newPos = Position2D + Velocity2D * dt;

				// check for wall collision (and for out-of-bounds)
				float BUFFER = 8f;
				float SQUARE_SIZE = grid.SquareSize;

				GridPosition gridPos = grid.GetGridPosFor2DPos( Position2D );
				Vector2 center = grid.Get2DPosForGridPos( gridPos );
				float left = center.x - SQUARE_SIZE * 0.5f + BUFFER;
				float right = center.x + SQUARE_SIZE * 0.5f - BUFFER;
				float down = center.y - SQUARE_SIZE * 0.5f + BUFFER;
				float up = center.y + SQUARE_SIZE * 0.5f - BUFFER;

				if ( newPos.x < Position2D.x )
				{
					if ( newPos.x < left && !grid.IsWalkable( grid.GetGridPosInDirection( gridPos, Direction.Left ) ) )
					{
						newPos = new Vector2( left, newPos.y );
						Velocity2D = new Vector2( -Velocity2D.x, Velocity2D.y );
					}
				}
				else if ( newPos.x > Position2D.x )
				{
					if ( newPos.x > right && !grid.IsWalkable( grid.GetGridPosInDirection( gridPos, Direction.Right ) ) )
					{
						newPos = new Vector2( right, newPos.y );
						Velocity2D = new Vector2( -Velocity2D.x, Velocity2D.y );
					}
				}

				if ( newPos.y < Position2D.y )
				{
					if ( newPos.y < down && !grid.IsWalkable( grid.GetGridPosInDirection( gridPos, Direction.Down ) ) )
					{
						newPos = new Vector2( newPos.x, down );
						Velocity2D = new Vector2( Velocity2D.x, -Velocity2D.y );
					}
				}
				else if ( newPos.y > Position2D.y )
				{
					if ( newPos.y > up && !grid.IsWalkable( grid.GetGridPosInDirection( gridPos, Direction.Up ) ) )
					{
						newPos = new Vector2( newPos.x, up );
						Velocity2D = new Vector2( Velocity2D.x, -Velocity2D.y );
					}
				}

				SetPosition2D( newPos );
				Velocity2D *= (1f - (IsInAir ? _deceleration : _groundedDeceleration));
			}
		}

		private void HandleHorizontalTargetMovement( float dt )
		{
			if ( IsInAir )
			{
				float progress = Utils.Map( _airTimer, 0f, _airTimeTotal, 0f, 1f );
				SetPosition2D( _horizontalStartingPos + (_horizontalTargetPos - _horizontalStartingPos) * progress );
			}
		}

		private void HandleVerticalPhysics( float dt )
		{
			if ( IsInAir )
			{
				// float landingHeight = _groundHeight + CollisionBounds.Size.y * 0.4f;
				float landingHeight = _groundHeight;

				float currentHeight = (_airTimer < _airTimeTotal * 0.5f)
					? Utils.Map( _airTimer, 0f, _airTimeTotal * 0.5f, _startingHeight, _peakHeight, EasingType.SineOut )
					: Utils.Map( _airTimer, _airTimeTotal * 0.5f, _airTimeTotal, _peakHeight, landingHeight, EasingType.SineIn );

				float currentRot = (_airTimer < _airTimeTotal * 0.5f)
					? Utils.Map( _airTimer, 0f, _airTimeTotal * 0.5f, _startingRotation, _startingRotation + (_targetRotation - _startingRotation) * 0.5f, EasingType.SineIn )
					: Utils.Map( _airTimer, _airTimeTotal * 0.5f, _airTimeTotal, _startingRotation + (_targetRotation - _startingRotation) * 0.5f, _targetRotation, EasingType.SineOut );

				_airTimer += dt;

				if ( _airTimer >= _airTimeTotal )
				{
					Position = new Vector3( Position.x, Position.y, landingHeight );
					Rotation = Rotation.From( currentRot, Rotation.Yaw(), Rotation.Roll() );

					IsInAir = false;
					HitGround();
				}
				else
				{
					Position = new Vector3( Position.x, Position.y, currentHeight );
					Rotation = global::Rotation.From( currentRot, Rotation.Yaw(), Rotation.Roll() );
				}
			}
		}

		public void UseHorizontalTargetPos( Vector2 targetPos )
		{
			_horizontalTargetPos = targetPos;
			_horizontalStartingPos = Position2D;
			_useHorizontalTargetPos = true;
		}

		public virtual void PlaceItem( Vector3 targetPos, float peakHeight, float airTimeTotal, int numFlips )
		{
			UseHorizontalTargetPos( Utils.GetVector2( targetPos ) );

			_startingHeight = Position.z;
			_peakHeight = peakHeight;
			_groundHeight = targetPos.z;
			_airTimeTotal = airTimeTotal;
			_airTimer = 0f;

			_startingRotation = Rotation.Pitch();
			_targetRotation = numFlips * 180f;

			MovementActive = true;
			IsInAir = true;

			AssignLifetime();
		}

		public virtual void Drop( Vector2 dir, float force, float peakHeightOffset, int numFlips )
		{
			SetPosition2D( new Vector2( Position.x, Position.y ) );
			Velocity2D = dir * force;

			_startingHeight = Position.z;
			_peakHeight = _startingHeight + peakHeightOffset;
			_groundHeight = 1f;
			_airTimeTotal = _peakHeight * 0.01f;
			_airTimer = 0f;

			_startingRotation = Rotation.Yaw();
			_targetRotation = numFlips * 180f;

			MovementActive = true;
			IsInAir = true;

			AssignLifetime();
		}

		protected virtual void HitGround()
		{
			// sfx

			HitGroundCallback?.Invoke( this );
		}

		public virtual void PersonStartedPickingUp( Person person )
		{
			PersonPickingUp = person;
		}

		public virtual void PersonFinishedPickingUp( Person person )
		{
			if ( PersonPickingUp == person )
				PersonPickingUp = null;
		}

		public virtual void PersonInterruptedPickingUp( Person person )
		{
			if ( PersonPickingUp == person )
				PersonPickingUp = null;
		}

		public virtual void SetIsHovered( bool hovered )
		{
			IsBeingHovered = hovered;
		}

		public virtual string GetHoverInfo() { return ""; }

		public virtual void AssignLifetime() { }
	}
}
