using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public enum StructureType { None, Wall, Fence, Factory, AmmoCacheBullets, AmmoCacheShells, AmmoCacheHPBullets, Turret }
	public enum BuildPhase { None, Structure, Direction }

	public partial class Structure : ModelEntity
	{
		public StructureManager StructureManager { get; set; }

		public StructureType StructureType { get; protected set; }
		public Direction FacingDirection { get; protected set; }

		[Net] public float MaxHp { get; protected set; }
		[Net] public float Hp { get; protected set; }
		[Net] public bool IsDestroyed { get; private set; }
		[Net] public bool IsBeingBuilt { get; set; }
		public bool IsHighlighted { get; protected set; }
		[Net] public bool ShowsHoverInfo { get; protected set; }

		public GridPosition GridPosition { get; private set; }
		public Vector2 Position2D { get; set; }

		public bool BlocksMovement { get; protected set; }
		public bool BlocksSight { get; protected set; }
		public bool BlocksGunshots { get; protected set; }
		public bool IsUpdateable { get; protected set; }

		[Net] public Player Owner { get; set; }

		public float Height { get; protected set; }

		public Structure( )
		{
			Transmit = TransmitType.Always;
		}

		public override void Spawn()
		{
			base.Spawn();

			SetupPhysicsFromModel( PhysicsMotionType.Static );
			EnableHitboxes = true;
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

		public virtual bool GetIsInteractable( Person person )
		{
			return false;
		}

		public virtual void Interact( Person person )
		{

		}

		public virtual void SetHovered( bool hovered )
		{
			if ( hovered )
			{
				if ( Local.Pawn is not Player player )  return;

				if ( player.Selected.Count == 1 )
				{
					if(!(player.Selected[0] is Survivor selectedSurvivor)) return;

					if ( GetIsInteractable( selectedSurvivor ) )
					{
						SetHighlighted( true );
					}
				}
			}
			else
			{
				SetHighlighted( false );
			}
		}

		public virtual void SetHighlighted( bool highlighted )
		{
			if ( IsHighlighted == highlighted ) return;

			IsHighlighted = highlighted;
			GlowActive = highlighted;
		}

		public static int GetCost( StructureType structureType )
		{
			int cost = 0;

			switch ( structureType )
			{
				case StructureType.Wall: cost = 10; break;
				case StructureType.Fence: cost = 20; break;
				case StructureType.AmmoCacheBullets: cost = 20; break;
				case StructureType.AmmoCacheShells: cost = 20; break;
				case StructureType.AmmoCacheHPBullets: cost = 20; break;
				case StructureType.Turret: cost = 30; break;
				case StructureType.Factory: cost = 50; break;
			}

			return cost;
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

			return buildTime * 0.01f;
			return buildTime;
		}

		public static string GetBuildingName( StructureType structureType )
		{
			string name = "";

			switch ( structureType )
			{
				case StructureType.Wall: name = "Wall"; break;
				case StructureType.Fence: name = "Fence"; break;
				case StructureType.AmmoCacheBullets: name = "Bullet Cache"; break;
				case StructureType.AmmoCacheShells: name = "Shell Cache"; break;
				case StructureType.AmmoCacheHPBullets: name = "High-Power Bullet Cache"; break;
				case StructureType.Turret: name = "Turret"; break;
				case StructureType.Factory: name = "Factory"; break;
			}

			return name;
		}

		public static string GetBuildingIcon( StructureType structureType )
		{
			string icon = "";

			switch ( structureType )
			{
				case StructureType.Wall: icon = "crop_din"; break;
				case StructureType.Fence: icon = "view_comfy"; break;
				case StructureType.AmmoCacheBullets: icon = "dashboard"; break;
				case StructureType.AmmoCacheShells: icon = "grid_view"; break;
				case StructureType.AmmoCacheHPBullets: icon = "dashboard_customize"; break;
				case StructureType.Turret: icon = "outbox"; break;
				case StructureType.Factory: icon = "select_all"; break;
			}

			return icon;
		}

		public virtual string GetHoverInfo()
		{
			return "";
		}
	}
}
