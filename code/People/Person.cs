using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Rcon;
using Entity = Sandbox.Entity;

namespace aftermath
{
	public delegate void PersonDelegate( Person person );

	public enum PersonType { None, Survivor, Zombie, Soldier }

	public partial class Person : AnimEntity, ISelectable
	{
		[Net] public string DebugText { get; set; }

		void DrawDebugText()
		{
			if ( string.IsNullOrEmpty( DebugText ) ) return;

			Color color = Player?.TeamColor?? Color.Black;
			float duration = 0f;
			float dist = 99999f;

			// DebugOverlay.Text( Position, 2, $"IsLocalPlayers: {IsLocalPlayers}, Local.Pawn: {Local.Pawn }\nIsServer: {IsServer}, IsSelected: {IsSelected}\nplayerNum: {PlayerNum}, networkIdent: {NetworkIdent}", color, duration, dist );
			DebugOverlay.Text( Position, 2, DebugText, color.WithAlpha( 0.5f ), duration, dist );
		}

		private struct AnimationValues
		{
			private float Speed;
			private bool Attacking;
			private int HoldType;

			public void Start()
			{
				Speed = 0f;
				HoldType = 0;
				Attacking = false;
			}

			public void Finish( AnimEntity entity )
			{
				entity.SetAnimInt( "holdtype", HoldType );
				entity.SetAnimBool( "attacking", Attacking );
				entity.SetAnimFloat( "speed", entity.GetAnimFloat( "speed" ).LerpTo( Speed, Time.Delta * 10f ) );
			}
		}

		[Net] public PersonType PersonType { get; protected set; }
		[Net] public bool IsMale { get; private set; }
		[Net] public string PersonName { get; protected set; }

		public Person_Movement Movement { get; private set; }
		public Person_Pathfinding Pathfinding { get; private set; }
		public Person_CommandHandler CommandHandler { get; private set; }
		public Person_RotationController RotationController { get; private set; }
		public Person_Aiming Aiming { get; private set; }
		public Person_GunHandler GunHandler { get; private set; }
		public Person_AmmoHandler AmmoHandler { get; private set; }

		[Net] public Player Player { get; private set; }
		[Net] public int PlayerNum { get; private set; }

		[Net] public Gun EquippedGun { get; set; }
		public Vector3 HeadPos => Position + new Vector3( 0f, 0f, 10f );

		public float MeleeRangeMin { get; protected set; }
		public float MeleeRangeMax { get; protected set; }
		public int MeleeDamage { get; protected set; }

		[Net] public bool IsAIControlled { get; protected set; }

		public float GunAimSpeedFactor => 1f;
		public float GunShootDelayFactor => 1f;
		public float GunShootTimeFactor => 1f;
		public float GunSpreadFactor => 1f;
		public float GunShootForceFactor => 1f;
		public float ReloadSpeedFactor => 1f;
		public float BuildSpeedFactor => 1f;

		public bool IsLocalPlayers
		{
			get
			{
				if ( IsServer ) return false;
				return (Local.Pawn as Player)?.PlayerNum == PlayerNum;
			}
		}

		public bool IsSelected { get; private set; }

		[Net] public bool IsDead { get; private set; }
		[Net] public bool IsSpawning { get; protected set; }

		private bool _shouldDrawPath;
		private List<Vector2> _path;

		public virtual List<Person> GetValidTargets() { return new List<Person>(); }

		public float MeleeAttackInaccuracy { get; protected set; }
		public float MeleeAttackDelayTime { get; protected set; }
		public float MeleeAttackAttackTime { get; protected set; }
		public float MeleeAttackPauseTime { get; protected set; }
		public float MeleeAttackRecoverTime { get; protected set; }

		public float RotationSpeed { get; protected set; }
		public float MeleeRotationSpeed { get; protected set; }
		public float CloseRangeDetectionDistance { get; protected set; }

		public float Hp { get; protected set; }
		public event PersonDelegate DiedCallback;

		[Net] public Vector2 Position2D { get; private set; }

		public void SetPosition2D( Vector2 pos )
		{
			if ( IsServer )
			{
				Position2D = pos;
				Position = new Vector3( pos.x, pos.y, Position.z );
			}
		}

		public Person()
		{
			Transmit = TransmitType.Always;
		}

		public override void Spawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			Movement = new Person_Movement { Person = this }; 
			Pathfinding = new Person_Pathfinding { Person = this };
			CommandHandler = new Person_CommandHandler { Person = this };
			RotationController = new Person_RotationController { Person = this };
			Aiming = new Person_Aiming { Person = this };
			GunHandler = new Person_GunHandler { Person = this };
			AmmoHandler = new Person_AmmoHandler { Person = this };

			CommandHandler.FinishedAllCommands += OnFinishAllCommands;

			CollisionGroup = CollisionGroup.Player;
			SetupPhysicsFromCapsule( PhysicsMotionType.Static, Capsule.FromHeightAndRadius( 64f, 10f ) ); // 8 radius default
			EnableHitboxes = true;

			// UseAnimGraph = false;

			Scale = 1.25f;

			IsMale = Rand.Float( 0f, 1f ) < (PersonType == PersonType.Soldier ? 0.8f : 0.5f);
			SetName( IsMale ? NameGenerator.GetRandomMaleName() : NameGenerator.GetRandomFemaleName() );

			MeleeRangeMin = 40f;
			MeleeRangeMax = 40f;
			MeleeAttackInaccuracy = 45f;
			MeleeDamage = 50;

			// item.SetParent( this, true );

			// var attachment = GetAttachment( "weapon", true );
			// if ( attachment.HasValue )
			// {
			// 	Log.Info( $"attachment: {attachment}" );
			// 	item.SetParent( this );
			// 	item.Position = attachment.Value.Position;
			// }
			// else
			// {
			// 	Log.Info( $"NO ATTACHMENT VALUE!: {attachment}" );
			// 	// item.Position = Position;
			// 	item.SetParent( this, true );
			// }
		}

		public virtual void Assign( Player player )
		{
			Log.Info( $"Person - Assign: {player}, color: {player?.TeamColor}" );
			Player = player;
			PlayerNum = player?.PlayerNum ?? 0;

			Movement.Init();
			Pathfinding.Init();
			CommandHandler.Init();
			RotationController.Init();
			Aiming.Init();
			GunHandler.Init();
		}

		protected void SetName( string name )
		{
			PersonName = name;
		}

		[Event.Tick.Server]
		protected virtual void Tick()
		{
			float dt = Time.Delta;

			Movement.Update( dt );
			Pathfinding.Update( dt );
			CommandHandler.Update( dt );
			RotationController.Update( dt );
			Aiming.Update( dt );

			// DebugText = $"RotationSpeed: {RotationController.RotationSpeed}";

			DebugText = $"Commands: {CommandHandler.CommandList.Count}";
			foreach ( var command in CommandHandler.CommandList )
			{
				DebugText += $"\n{command.ToString()}";
			}
			
			DebugText += $"\nHP:{Hp.FloorToInt()}";
		}

		[Event.Tick.Client]
		protected virtual void ClientTick()
		{
			float dt = Time.Delta;

			if ( IsSelected && IsLocalPlayers )
			{
				Utils.DrawCircle( Position2D, 7f + MathF.Sin( Time.Now * 2f ) * 6f, 25f, 12, Player.TeamColor, Time.Now * -4f );
			}

			if ( _shouldDrawPath )
				RenderPath();

			DrawDebugText();
		}

		// CLIENT
		public virtual void Select()
		{
			IsSelected = true;
			Person.SetSelected( true, NetworkIdent );
		}

		public virtual void Deselect()
		{
			IsSelected = false;
			Person.SetSelected( false, NetworkIdent );
		}

		[ServerCmd]
		public static void SetSelected( bool selected, int id )
		{
			var person = Entity.FindByIndex( id ) as Person;
			person?.SetSelected( selected );
		}

		// SERVER
		public void SetSelected( bool selected )
		{
			IsSelected = selected;
		}

		public void MoveTo( Vector3 targetPos )
		{
			Host.AssertServer();

			Vector2 movePos = new Vector2( targetPos.x, targetPos.y );
			bool valid = AftermathGame.Instance.GridManager.ImproveMovePosition( ref movePos );

			if ( valid )
			{
				MoveToPosCommand moveCommand = new MoveToPosCommand( new Vector2( movePos.x, movePos.y ) );
				CommandHandler.SetCommand( moveCommand );
			}
		}

		[ServerCmd]
		public static void MoveTo( Vector2 pos, int id )
		{
			// var caller = ConsoleSystem.Caller.Pawn as Player;
			// Log.Info( $"MoveTo - pos: {pos}, id: {id}, caller: {caller}, num selected: {caller.Selected.Count}" );

			var person = Entity.FindByIndex( id ) as Person;
			person?.MoveTo( pos );
		}

		[ServerCmd]
		public static void MoveToPickUpItem( int itemId, int personId )
		{
			if ( Entity.FindByIndex( itemId ) is not Item item ) return;
			if ( Entity.FindByIndex( personId ) is not Person person ) return;

			MoveToPosCommand moveCommand = new( item.Position2D );
			MoveToPickUpItemCommand moveToPickUpCommand = new( item );
			ParallelCommand parallelCommand = new(new List<PersonCommand>() {moveCommand, moveToPickUpCommand});

			person.CommandHandler.SetCommand( parallelCommand );
		}

		[ServerCmd]
		public static void MoveToBuild( Vector2 pos, int gridX, int gridY, StructureType structureType, Direction structureDirection, int cost, int personId )
		{
			if ( Entity.FindByIndex( personId ) is not Person person ) return;

			MoveToPosCommand moveCommand = new MoveToPosCommand( pos, default(GridPosition) );
			MoveToBuildCommand moveToBuildCommand = new MoveToBuildCommand( new GridPosition(gridX, gridY), structureType, structureDirection, cost );
			ParallelCommand parallelCommand = new ParallelCommand( new List<PersonCommand>() {moveCommand, moveToBuildCommand} );
			person.CommandHandler.SetCommand( parallelCommand );
		}

		[ServerCmd]
		public static void DropGun( int id )
		{
			var person = Entity.FindByIndex( id ) as Person;
			person?.GunHandler?.DropGun( Vector2.Right, 40f, Rand.Float( 1f, 3f ), 8 );
		}

		[ServerCmd]
		public static void Attack( int id )
		{
			var person = Entity.FindByIndex( id ) as Person;
			person?.SetAnimBool( "b_attack", true );
			person?.SetAnimFloat( "aimat_weight", 0.5f );
			person?.SetAnimFloat( "holdtype_attack", 2f );

			Log.Info( $"person: {person}" );
		}

		public virtual bool ShouldRepelFromAllies()
		{
			return !AftermathGame.Instance.GridManager.IsEdgeGridPos( Movement.CurrentGridPos );
		}

		[ClientRpc]
		public void DrawPath( Vector2[] path )
		{
			_shouldDrawPath = true;
			_path = path.ToList();
		}

		[ClientRpc]
		public void StopDrawingPath()
		{
			_shouldDrawPath = false;
		}

		void RenderPath()
		{
			if ( _path.Count == 0 )
				return;

			DebugOverlay.Line( AftermathGame.Instance.GridManager.GetWorldPosFor2DPos( Position2D ).WithZ( 0.01f ), AftermathGame.Instance.GridManager.GetWorldPosFor2DPos( _path[0] ).WithZ( 0.01f ), Player.TeamColor);

			for ( int i = 0; i < _path.Count - 1; i++ )
				DebugOverlay.Line( AftermathGame.Instance.GridManager.GetWorldPosFor2DPos( _path[i] ).WithZ( 0.01f ), AftermathGame.Instance.GridManager.GetWorldPosFor2DPos( _path[i + 1] ).WithZ( 0.01f ), Player.TeamColor );
		}

		protected virtual void OnFinishAllCommands( Person_CommandHandler commandHandler )
		{
			
		}

		public virtual void FoundTarget( Person target )
		{
			AftermathGame.Instance.SpawnFloater( Position, $"FoundTarget {target.PersonName}!", new Color( 1f, 0.4f, 0.8f, 0.2f ) );
			// DebugOverlay.Line( Position, target.Position, Color.Red, 1f );
		}

		public virtual void LostTarget( Person target, Vector2 lastSeenPos )
		{

		}

		public virtual void MeleeAttack( Vector2 dir, Person target )
		{

		}

		public void HitByGunshot( Gunshot gunshot, Vector3 hitPos, bool penetrate )
		{
			Hp -= gunshot.Damage;

			Movement.AddForceVelocity( Utils.GetVector2( gunshot.Direction ) * gunshot.BulletForce );

			if ( Hp <= 0f )
			{
				Die( gunshot.Direction, gunshot.ShootingPerson );
			}
		}

		public void HitByMelee( Person attacker, Vector3 hitPos )
		{
			Die( HeadPos - attacker.HeadPos, attacker );
		}

		public virtual void Die( Vector3 force, Person killer )
		{
			if(IsDead) return;

			IsDead = true;

			CommandHandler.ClearCommands();

			if ( GunHandler.HasGun )
			{
				GunHandler.DropGun(
					new Vector2( Rand.Float( -1f, 1f ), Rand.Float( -1f, 1f ) ).Normal,
					Rand.Float( Person_GunHandler.TOSS_FORCE_MIN, Person_GunHandler.TOSS_FORCE_MAX ),
					Rand.Float( 5f, 10f ),
					Rand.Int( Person_GunHandler.TOSS_NUM_FLIPS_MIN, Person_GunHandler.TOSS_NUM_FLIPS_MAX )
				);
			}

			AmmoHandler.DropAllAmmo();

			DiedCallback?.Invoke( this );
			AftermathGame.Instance.PersonManager.PersonDied( this );
		}
	}
}
