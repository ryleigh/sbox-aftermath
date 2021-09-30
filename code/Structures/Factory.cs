using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public partial class Factory : Structure
	{
		public static Vector3 SlotPos = new Vector3( 0f, 1.5f, 0f ) / 16f;

		private float _productionTimer;
		private readonly float PRODUCTION_TIME_MIN = 4f;
		private readonly float PRODUCTION_TIME_MAX = 10f;

		public Factory()
		{
			BlocksMovement = true;
			BlocksSight = true;
			BlocksGunshots = true;

			Height = 72f;

			MaxHp = 100f;
			Hp = MaxHp;

			ShowsHoverInfo = true;
			IsUpdateable = true;
			StructureType = StructureType.Factory;
		}

		public override void Spawn()
		{
			SetModel( "models/square_wooden_box.vmdl" );
			Scale = 1.83f;
			RenderColor = new Color( 0f, 0f, 0f );
			_productionTimer = Rand.Float( PRODUCTION_TIME_MIN, PRODUCTION_TIME_MAX );
		}

		public override void Update( float dt )
		{
			base.Update( dt );

			if ( !IsBeingBuilt && !IsDestroyed )
			{
				HandleProduction( dt );
			}
		}

		private void HandleProduction( float dt )
		{
			_productionTimer -= dt;
			if ( _productionTimer <= 0f )
			{
				ProduceAmmo();
				Damage(Rand.Int( 6, 10 ), Vector2.Zero);
				_productionTimer = Rand.Float( PRODUCTION_TIME_MIN, PRODUCTION_TIME_MAX );
			}
		}

		void ProduceAmmo()
		{
			float rand = Rand.Float( 0f, 1f );
			AmmoType ammoType;
			int ammoAmount;

			if ( rand <= 0.43f )
			{
				ammoType = AmmoType.Bullet;
				ammoAmount = Rand.Int( 5, 11 );
			}
			else if ( rand <= 0.73f )
			{
				ammoType = AmmoType.Shell;
				ammoAmount = Rand.Int( 3, 6 );
			}
			else if ( rand <= 0.96f )
			{
				ammoType = AmmoType.HPBullet;
				ammoAmount = Rand.Int( 2, 5 );
			}
			else
			{
				ammoType = AmmoType.Grenade;
				ammoAmount = Rand.Int( 1, 3 );
			}

			GridPosition adjacentGridPos = AftermathGame.Instance.GridManager.GetAdjacentEmptyGridPositionDiagonal( GridPosition );
			if ( adjacentGridPos.IsValid )
			{
				Vector2 targetPos = AftermathGame.Instance.GridManager.Get2DPosForGridPos( adjacentGridPos ) + new Vector2( Rand.Float( -0.3f, 0.3f ), Rand.Float( -0.3f, 0.3f ) ) * AftermathGame.Instance.GridManager.SquareSize;
				AftermathGame.Instance.CreateAmmoItem( Position + SlotPos, targetPos, ammoType, ammoAmount, Rand.Float( 70f, 100f ), Rand.Float( 0.9f, 1.4f ), 0 );
			}
		}
	}
}
