using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public partial class AmmoItem : Item
	{
		[Net] public AmmoType AmmoType { get; set; }
		[Net] public int AmmoAmount { get; set; }
		[Net] public int MaxAmount { get; protected set; }

		public Person CarryingPerson { get; private set; }

		public AmmoItem()
		{

		}

		public override void Spawn()
		{
			ModelPath = "models/maya_testcube_100.vmdl";
			Scale = 0.125f;

			base.Spawn();
		}

		public void Init( AmmoType ammoType, int ammoAmount )
		{
			AmmoType = ammoType;

			if ( ammoType == AmmoType.Bullet )
			{
				RenderColor = new Color( 1f, 1f, 0f );
				MaxAmount = 50;
			}
			else if ( ammoType == AmmoType.Shell )
			{
				RenderColor = new Color( 1f, 0.5f, 0f );
				MaxAmount = 50;
			}
			else if ( ammoType == AmmoType.HPBullet )
			{
				RenderColor = new Color( 0.2f, 0.2f, 1f );
				MaxAmount = 50;
			}
			else if ( ammoType == AmmoType.Grenade )
			{
				RenderColor = new Color( 0.3f, 0.3f, 0.3f );
				MaxAmount = 25;
			}

			SetAmmoAmount( Math.Min( ammoAmount, MaxAmount ) );
		}

		protected override void Tick()
		{
			float dt = Time.Delta;

			// DebugText = GetHoverInfo();

			if ( CarryingPerson != null )
			{
				Position = CarryingPerson.Position + CarryingPerson.Rotation.Forward * -5f + CarryingPerson.Rotation.Up * 50f;
				Rotation = CarryingPerson.Rotation;
			}

			base.Tick();
		}

		public override void PersonStartedPickingUp( Person person )
		{
			base.PersonStartedPickingUp( person );
		}

		public override void PersonFinishedPickingUp( Person person )
		{
			base.PersonFinishedPickingUp( person );

			person.AmmoHandler.AddAmmo( this );
		}

		public virtual void SetAmmoAmount( int amount )
		{
			AmmoAmount = amount;
		}

		public override string GetHoverInfo()
		{
			string str = "";
			if ( AmmoType == AmmoType.Bullet )
				str = AmmoAmount + (AmmoAmount == 1 ? " Bullet" : " Bullets");
			else if ( AmmoType == AmmoType.Shell )
				str = AmmoAmount + (AmmoAmount == 1 ? " Shell" : " Shells");
			else if ( AmmoType == AmmoType.HPBullet )
				str = AmmoAmount + (AmmoAmount == 1 ? " High-Powered Bullet" : " High-Powered Bullets");
			else if ( AmmoType == AmmoType.Grenade )
				str = AmmoAmount + (AmmoAmount == 1 ? " Grenade" : " Grenades");

			return str;
		}

		public void SetCarryingPerson( Person person )
		{
			CarryingPerson = person;
			PhysicsEnabled = false;
			EnableHitboxes = false;
		}

		public void RemoveCarryingPerson()
		{
			if ( CarryingPerson == null ) return;

			CarryingPerson = null;
			PhysicsEnabled = true;
			EnableHitboxes = true;
		}

		public override void AssignLifetime()
		{
			_lifetime = Utils.Map( (float)AmmoAmount, 1f, 100f, 15f, 40f, EasingType.CubicOut );

			if ( AmmoType == AmmoType.Grenade )
				_lifetime *= 1.5f;
		}

		protected override bool AllowedToDespawn()
		{
			return CarryingPerson == null && !IsInAir && !IsBeingHovered && !IsBeingPickedUp && NumPeopleMovingToPickUp == 0 && Velocity2D.LengthSquared < 0.01f;
		}
	}
}
