using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Internal;

namespace aftermath
{
	public partial class AftermathGame : Sandbox.Game
	{
		public static AftermathGame Instance { get; private set; }

		[Net] public GridManager GridManager { get; private set; }
		[Net] public PersonManager PersonManager { get; private set; }
		[Net] public StructureManager StructureManager { get; private set; }

		public MinimalHudEntity Hud;

		public AftermathGame()
		{
			Instance = this;

			if ( IsServer )
			{
				GridManager = new GridManager();
				PersonManager = new PersonManager();
				StructureManager = new StructureManager();
			}

			if ( IsClient )
			{
				Hud = new MinimalHudEntity();
			}
		}

		[Event.Hotload]
		public void HotloadUpdate()
		{
			if ( !IsClient ) return;

			Hud?.Delete();
			Hud = new MinimalHudEntity();
		}
		
		public override void Spawn()
		{
			base.Spawn();

		}

		// SERVER
		public override void OnActive()
		{
			base.OnActive();

		}

		// SERVER
		public override void OnClientActive()
		{
			base.OnClientActive();

			GridManager.Initialize( 32, 32 );
			PersonManager.Initialize();
			StructureManager.Initialize();
		}

		// CLIENT
		public override void ClientSpawn()
		{
			base.ClientSpawn();

			GridManager.Initialize( 32, 32 );
			PersonManager.Initialize();
			StructureManager.Initialize();
		}

		// SERVER
		public override void ClientJoined( Client client )
		{
			Log.Warning( $"________________ \"{client.Name}\" has joined the game. NetworkIdent: {client.NetworkIdent}, IsServer: {IsServer}" );

			var player = new Player();
			client.Pawn = player;
			MoveToSpawnpoint( client.Pawn );

			Rounds.Current?.OnPlayerJoin( player );

			base.ClientJoined( client );
		}

		// SERVER
		public override void ClientDisconnect( Client client, NetworkDisconnectionReason reason )
		{
			Log.Info( $"\"{client.Name}\" has disconnected for reason: {reason}. NetworkIdent: {client.NetworkIdent}" );

			Rounds.Current?.OnPlayerLeave( client.Pawn as Player );

			base.ClientDisconnect( client, reason );
		}

		public override CameraSetup BuildCamera( CameraSetup camSetup )
		{
			camSetup.Rotation = Local.Client.Pawn.EyeRot;
			camSetup.Position = Local.Client.Pawn.EyePos;

			return base.BuildCamera( camSetup );
		}

		[Event.Tick]
		protected void Tick()
		{
			if ( IsServer )
			{
				StructureManager.Update( Time.Delta );
			}
		}

		public async Task StartSecondTimer()
		{
			while ( true )
			{
				await Task.DelaySeconds( 1 );
				OnSecond();
			}
		}

		public override void PostLevelLoaded()
		{
			_ = StartSecondTimer();

			base.PostLevelLoaded();
		}

		private void OnSecond()
		{
			CheckMinimumPlayers();
		}

		private void CheckMinimumPlayers()
		{
			if ( Client.All.Count >= 2 )
			{
				if ( Rounds.Current is LobbyRound || Rounds.Current == null )
				{
					Rounds.Change( new PlayRound() );
				}
			}
			else if ( Rounds.Current is not LobbyRound )
			{
				Rounds.Change( new LobbyRound() );
			}
		}
	}
}
