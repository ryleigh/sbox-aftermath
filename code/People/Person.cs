using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Rcon;
using Entity = Sandbox.Entity;

namespace aftermath
{
	public enum PersonType { None, Survivor, Zombie, Soldier }

	public partial class Person : AnimEntity, ISelectable
	{
		[Net] public string DebugText { get; set; }

		void DrawDebugText()
		{
			Color color = Player?.TeamColor?? Color.Black;
			float duration = 0f;
			float dist = 99999f;

			// DebugOverlay.Text( Position, 2, $"IsLocalPlayers: {IsLocalPlayers}, Local.Pawn: {Local.Pawn }\nIsServer: {IsServer}, IsSelected: {IsSelected}\nplayerNum: {PlayerNum}, networkIdent: {NetworkIdent}", color, duration, dist );
			DebugOverlay.Text( Position, 2, DebugText, color, duration, dist );
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

		public Person_Movement Movement { get; private set; }
		public Person_Pathfinding Pathfinding { get; private set; }
		public Person_CommandHandler CommandHandler { get; private set; }
		public Person_RotationController RotationController { get; private set; }
		public Person_Aiming Aiming { get; private set; }

		[Net] public Player Player { get; private set; }
		[Net] public int PlayerNum { get; private set; }

		[Net] public bool IsAIControlled { get; protected set; }

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

		[Net] public Vector2 Position2D { get; private set; }
		private bool _shouldDrawPath;
		private List<Vector2> _path;

		public virtual List<Person> GetValidTargets() { return new List<Person>(); }

		public float CloseRangeDetectionDistance { get; protected set; }

		public void SetPosition2D( Vector2 pos )
		{
			if ( IsServer )
			{
				Position2D = pos;
				Position = new Vector3( pos.x, pos.y, 0f );
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

			CommandHandler.FinishedAllCommands += OnFinishAllCommands;

			RotationController.RotationSpeed = 2f;

			CollisionGroup = CollisionGroup.Player;
			SetupPhysicsFromCapsule( PhysicsMotionType.Dynamic, Capsule.FromHeightAndRadius( 64f, 26f ) ); // 8 radius default
			EnableHitboxes = true;

			Scale = 1.25f;
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

			DebugText = $"Commands: {CommandHandler.CommandList.Count}";
			foreach ( var command in CommandHandler.CommandList )
			{
				DebugText += $"\n{command.ToString()}";
			}
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

		public virtual void Select()
		{
			IsSelected = true;
		}

		public virtual void Deselect()
		{
			IsSelected = false;
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

		}
	}
}
