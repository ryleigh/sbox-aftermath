using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class MoveToPickUpItemCommand : PersonCommand
	{
		public Item Item { get; private set; }

		private const float REQ_DISTANCE = 30f;

		public override string ToString() { return $"MoveToPickUpItem: {Item?.GetHoverInfo() ?? "NONE"}"; }

		public MoveToPickUpItemCommand( Item item )
		{
			Type = PersonCommandType.MoveToPickUpItem;
			Item = item;
		}

		public override void Begin()
		{
			base.Begin();

			Item.NumPeopleMovingToPickUp++;

			if ( Item == null || Item.IsInAir || Item.IsBeingPickedUp || Person == null || Person.IsDead )
			{
				Finish();
				return;
			}
		}

		public override void Update( float dt )
		{
			if(IsFinished)
				return;

			base.Update( dt );

			if ( Item == null || Item.IsInAir || Item.IsBeingPickedUp || Person == null || Person.IsDead )
			{
				Finish();
			}

			CheckDistance();
		}

		void CheckDistance()
		{
			float distSqr = (Item.Position2D - Person.Position2D).LengthSquared;
			if ( distSqr <= REQ_DISTANCE * REQ_DISTANCE )
			{
				Person.CommandHandler.SetCommand( new PickUpItemCommand(Item ) );
			}
		}

		public override void Finish()
		{
			base.Finish();

			Item.NumPeopleMovingToPickUp--;
		}

		public override void Interrupt()
		{
			base.Interrupt();

			Item.NumPeopleMovingToPickUp--;
		}
	}
}
