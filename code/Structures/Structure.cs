using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public enum StructureType { None, Wall, Fence, Factory, AmmoCacheBullets, AmmoCacheShells, AmmoCacheHPBullets, Turret }

	public partial class Structure : ModelEntity
	{
		public StructureManager StructureManager { get; set; }

		public StructureType StructureType { get; protected set; }
		public Direction FacingDirection { get; protected set; }

		public float MaxHp { get; protected set; }
		public float Hp { get; protected set; }
		public bool IsDestroyed { get; private set; }
		public bool IsBeingBuilt { get; set; }

		public GridPosition GridPosition { get; private set; }
		public Vector2 Position2D { get; set; }

		public bool BlocksMovement { get; protected set; }
		public bool BlocksSight { get; protected set; }
		public bool BlocksGunshots { get; protected set; }
		public bool IsUpdateable { get; protected set; }

		public float Height { get; protected set; }

		public Structure( )
		{
			Transmit = TransmitType.Always;
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();

			// Log.Warning( $"Structure - ClientSpawn **************** IsServer: {Host.IsServer}, StructureManager: {AftermathGame.Instance.StructureManager}" );
		}

		public virtual void Update( float dt )
		{

		}

		public void HitByGunshot( Gunshot gunshot, Vector3 hitPos )
		{
			Damage( gunshot.Damage, Utils.GetVector2( gunshot.Direction ) );
		}

		public virtual void MeleeAttacked( Person person, Vector3 pos )
		{
			Damage( person.MeleeDamage, (Position2D - person.Position2D).Normal );
		}

		public virtual void Damage( float damage, Vector2 direction )
		{
			Hp -= damage;
			if ( Hp <= 0f )
			{
				Destroy( direction );
			}
		}

		public virtual void Destroy( Vector2 direction )
		{
			if( IsDestroyed ) return;

			IsDestroyed = true;
			AftermathGame.Instance.StructureManager.RemoveStructure( this );
		}

		public virtual void SetGridPos( GridPosition gridPos )
		{
			GridPosition = gridPos;
			Position2D = AftermathGame.Instance.GridManager.Get2DPosForGridPos( gridPos );

			if(IsServer)
				Position = AftermathGame.Instance.GridManager.GetWorldPosForGridPos( gridPos );
		}

		public virtual void SetDirection( Direction direction )
		{
			FacingDirection = direction;
		}

		public static float GetBuildTime( StructureType structureType )
		{
			float buildTime = 0f;

			switch ( structureType )
			{
				case StructureType.Wall: buildTime = 10f; break;
				case StructureType.Fence: buildTime = 15f; break;
				case StructureType.AmmoCacheBullets: buildTime = 12f; break;
				case StructureType.AmmoCacheShells: buildTime = 12f; break;
				case StructureType.AmmoCacheHPBullets: buildTime = 12f; break;
				case StructureType.Turret: buildTime = 15f; break;
				case StructureType.Factory: buildTime = 20f; break;
			}

			return buildTime;
		}
	}
}
