using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class ShootCommand : PersonCommand
	{
		public Person Target { get; private set; }
		public bool HasTarget => (Target != null);

		private float _shootTimer;

		public Gun Gun { get; private set; }

		public override string ToString() { return "ShootCommand: " + (Target != null ? Target.PersonName : "NONE"); }

		public ShootCommand( Person target )
		{
			Target = target;

			Type = PersonCommandType.Shoot;
		}

		public override void Begin()
		{
			base.Begin();

			Gun = Person.GunHandler.Gun;

			if ( Person.IsDead || Target.IsDead || Gun == null )
			{
				Finish();
				return;
			}

			_shootTimer = Rand.Float( Gun.ShootTimeMin, Gun.ShootTimeMax ) * Person.GunShootTimeFactor;

			// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.None );
		}

		public override void Update( float dt )
		{
			if( IsFinished ) return;

			base.Update( dt );

			if ( Person.IsDead || Target.IsDead || Gun == null )
			{
				Finish();
				return;
			}

			_shootTimer -= dt;
			if ( _shootTimer <= 0f )
			{
				if ( !Gun.Shoot() )
				{
					Person.GunHandler.OutOfAmmo();
				}

				Finish();
			}
		}
	}
}
