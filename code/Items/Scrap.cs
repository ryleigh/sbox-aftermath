using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public partial class Scrap : Item
	{
		[Net] public int Amount { get; private set; }

		private readonly float ATTRACT_DIST_SQR = MathF.Pow( 100f, 2f );
		private readonly float PICKUP_DIST_SQR = MathF.Pow( 10f, 2f );
		private readonly float ATTRACT_FORCE = 100f;

		private float _delay;

		public Scrap()
		{
			_disablePhysicsOnRest = false;
		}

		public override void Spawn()
		{
			// ModelPath = $"models/sbox_props/gas_cylinder_fat/metal_gib_{Rand.Int( 2, 4 )}.vmdl";
			ModelPath = $"models/rust_props/barrels/lootbarrel_gibs_07.vmdl";

			base.Spawn();
		}

		public void Init( int amount )
		{
			Amount = amount;
			Scale = Utils.Map( amount, 0, 20, 1f, 2f, EasingType.SineIn );
			_delay = 0.15f;

			AssignLifetime();
		}

		protected override void Tick()
		{
			base.Tick();

			float dt = Time.Delta;

			// DebugText = $"PhysicsActive: {PhysicsActive}";

			if ( !IsInAir )
			{
				if ( _delay <= 0f )
					HandleAttraction( dt );
				else
					_delay -= dt;
			}
		}

		private void HandleAttraction( float dt )
		{
			var survivors = Entity.All.OfType<Survivor>()
					.Where( s => !s.IsDead )
					.Where( s => !s.IsSpawning )
					.Where( s => (s.Position2D - Position2D).LengthSquared < ATTRACT_DIST_SQR )
					.OrderBy( s => (s.Position2D - Position2D).LengthSquared )
					.ToList();

			if ( survivors.Count > 0 )
			{
				Vector2 survivorPos = survivors[0].Position2D;
				float sqrDist = (survivorPos - Position2D).LengthSquared;

				if ( sqrDist < PICKUP_DIST_SQR )
				{
					PickUp( survivors[0] );
				}
				else
				{
					float force = Utils.Map( sqrDist, ATTRACT_DIST_SQR, 0f, 0f, ATTRACT_FORCE, EasingType.SineIn );
					Velocity2D += (survivorPos - Position2D) * force * dt;
				}
			}
		}

		private void PickUp( Person person )
		{
			if ( person?.Player == null ) return;

			person.Player.AdjustScrapAmount( Amount );
			AftermathGame.Instance.SpawnFloater( Position, $"+{Amount} Scrap", Color.White );

			Delete();
		}

		public override void AssignLifetime()
		{
			_lifetime = Utils.Map( (float)Amount, 1f, 50f, 15f, 30f, EasingType.SineOut );
		}

		protected override void LifetimeFinished()
		{
			base.LifetimeFinished();
			// CreateSmoke( MathF.Random( 3, 4 ) );
		}

		public override string GetHoverInfo()
		{
			return $"{Amount} Scrap";
		}
	}
}
