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
	public class PickUpItemCommand : PersonCommand
	{
		public Item Item { get; private set; }

		private float _pickupTimer;
		private const float PICKUP_TIME_MIN = 0.08f;
		private const float PICKUP_TIME_MAX = 0.12f;
		private float _pickupTimeTotal;

		private Vector2 _startingPos;
		private Rotation _startingRot;

		private bool _facingItem = false;

		public override string ToString() { return $"PickUpItem: {Item?.GetHoverInfo() ?? "NONE"} ... {_pickupTimer} / {_pickupTimeTotal}"; }

		public PickUpItemCommand( Item item )
		{
			Type = PersonCommandType.PickUpItem;
			Item = item;
		}

		public override void Begin()
		{
			base.Begin();

			if ( Item == null || Item.IsBeingPickedUp || Person == null || Person.IsDead )
			{
				Item = null;
				Finish();
				return;
			}

			Item.PersonStartedPickingUp( Person );
		}

		public override void Update( float dt )
		{
			if ( IsFinished )
				return;

			base.Update( dt );

			if ( Item == null || Person == null || Person.IsDead )
			{
				Finish();
				return;
			}

			if ( _facingItem )
				PickUpItem( dt );
			else
				_facingItem = CheckFacing();
		}

		bool CheckFacing()
		{
			float dot = (float)Vector2.GetDot( Person.Aiming.BodyDirection, (Item.Position2D - Person.Position2D).Normal );
			// DebugOverlay.Line(Person.Position.WithZ( 0.1f ), Person.Position.WithZ( 0.1f ) + (Vector3)Person.Aiming.SightDirection * 100f, Color.Blue);
			// DebugOverlay.Line( Person.Position.WithZ( 0.1f ), Person.Position.WithZ( 0.1f ) + (Vector3)( Item.Position2D - Person.Position2D).Normal * 100f, Color.Cyan);

			if ( dot > 0.75f )
			{
				StartAnimation();
				return true;
			}

			return false;
		}

		void StartAnimation()
		{
			_startingPos = Item.Position;
			_startingRot = Item.Rotation;
			_pickupTimer = 0f;
			_pickupTimeTotal = Rand.Float( PICKUP_TIME_MIN, PICKUP_TIME_MAX );
		}

		void PickUpItem( float dt )
		{
			_pickupTimer += dt;
			float progress = Utils.Map( _pickupTimer, 0f, _pickupTimeTotal, 0f, 1f, EasingType.SineIn );
			Item.Position = Vector3.Lerp( _startingPos, Person.Position, progress );
			Item.Rotation = Rotation.Lerp( _startingRot, Person.Rotation, progress );

			if(_pickupTimer >= _pickupTimeTotal)
				Finish();
		}

		public override void Finish()
		{
			base.Finish();

			if (Item != null)
				Item.PersonFinishedPickingUp( Person );
		}

		public override void Interrupt()
		{
			base.Interrupt();

			if ( Item != null )
				Item.PersonInterruptedPickingUp( Person );
		}
	}
}
