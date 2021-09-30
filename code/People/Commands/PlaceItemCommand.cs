using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public delegate void PlaceItemDelegate( Person person, Item item );

	public class PlaceItemCommand : PersonCommand
	{
		public Item Item { get; private set; }

		private float _placeTimer;
		private const float PLACE_TIME_MIN = 0.14f;
		private const float PLACE_TIME_MAX = 0.16f;
		private float _placeTimeTotal;

		private readonly Vector3 _targetPos;
		private readonly float _peakHeight;
		private readonly int _numFlips;

		private bool _facingTarget = false;

		private bool _hasStartedPlacing;
		private bool _hasFinishedPlacing;

		public event PlaceItemDelegate StartPlacingItem;
		public event PlaceItemDelegate FinishPlacingItem;

		private readonly bool _lookAtDestination;
		private readonly bool _requireFacing;

		public override string ToString() { return $"PlaceItem: {Item}"; }

		public PlaceItemCommand( Item item, Vector3 targetPos, float peakHeight, int numFlips, bool lookAtDestination = true, bool requireFacing = true )
		{
			Type = PersonCommandType.PlaceItem;
			Item = item;

			_targetPos = targetPos;
			_peakHeight = peakHeight;
			_numFlips = numFlips;
			_lookAtDestination = lookAtDestination;
			_requireFacing = requireFacing;
		}

		public override void Begin()
		{
			base.Begin();

			if ( Item == null || Person == null || Person.IsDead )
			{
				Item = null;
				Finish();
				return;
			}

			Vector2 targetPos = _lookAtDestination ? Utils.GetVector2( _targetPos ) - Person.Position2D : Item.Position2D - Person.Position2D;
			Person.Aiming.SetSightDirection( targetPos );
			Person.Aiming.SetTargetSightDirection( targetPos );
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

			if ( _requireFacing )
			{
				if ( _facingTarget )
				{
					PlaceItem( dt );
				}
				else
				{
					_facingTarget = CheckFacing();
					if ( _facingTarget )
						StartPlacing();
				}
			}
			else
			{
				if ( _hasStartedPlacing )
				{
					PlaceItem( dt );
				}
				else
				{
					StartPlacing();
				}
			}
		}

		bool CheckFacing()
		{
			float dot = Person.Rotation.Forward.Dot( (_targetPos - Person.Position).Normal );
			if ( dot > 0.95f )
			{
				return true;
			}

			return false;
		}

		void StartPlacing()
		{
			if ( _hasStartedPlacing )
				return;

			StartPlacingItem?.Invoke( Person, Item );
			_hasStartedPlacing = true;

			_placeTimeTotal = Rand.Float( PLACE_TIME_MIN, PLACE_TIME_MAX );

			Item.PlaceItem( _targetPos, _peakHeight, _placeTimeTotal, _numFlips );
		}

		void PlaceItem( float dt )
		{
			_placeTimer += dt;
			if ( _placeTimer >= _placeTimeTotal )
			{
				Finish();
			}
		}

		public override void Finish()
		{
			base.Finish();

			if ( !_hasStartedPlacing )
			{
				StartPlacingItem?.Invoke( Person, Item );
				_hasStartedPlacing = true;
			}

			if ( !_hasFinishedPlacing )
			{
				FinishPlacingItem?.Invoke( Person, Item );
				_hasFinishedPlacing = true;
			}
		}

		public override void Interrupt()
		{
			base.Interrupt();

			if ( !_hasStartedPlacing )
			{
				StartPlacingItem?.Invoke( Person, Item );
				_hasStartedPlacing = true;
			}

			if ( !_hasFinishedPlacing )
			{
				FinishPlacingItem?.Invoke( Person, Item );
				_hasFinishedPlacing = true;
			}
		}
	}
}
