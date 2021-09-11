﻿using Sandbox;
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
			if ( string.IsNullOrEmpty( DebugText ) ) return;

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

		[Net] public bool IsAIControlled { get; protected set; }

		public float GunAimSpeedFactor => 1f;
		public float GunShootDelayFactor => 1f;
		public float GunShootTimeFactor => 1f;

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

		public float CloseRangeDetectionDistance { get; protected set; }

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

			RotationController.RotationSpeed = 3f;

			CollisionGroup = CollisionGroup.Player;
			SetupPhysicsFromCapsule( PhysicsMotionType.Static, Capsule.FromHeightAndRadius( 64f, 10f ) ); // 8 radius default
			EnableHitboxes = true;

			// UseAnimGraph = false;

			Scale = 1.25f;

			Gun gun = new Pistol();
			GunHandler.StartEquippingGun( gun );
			GunHandler.FinishEquippingGun( gun );

			IsMale = Rand.Float( 0f, 1f ) < (PersonType == PersonType.Soldier ? 0.8f : 0.5f);
			SetName( IsMale ? NameGenerator.GetRandomMaleName() : NameGenerator.GetRandomFemaleName() );

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
			// DebugOverlay.Line( Position, target.Position, Color.Red, 1f );
		}
	}
}
