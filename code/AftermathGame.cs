using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Internal;
using System.Linq;

namespace aftermath
{
	public partial class AftermathGame : Sandbox.Game
	{
		public static AftermathGame Instance { get; private set; }

		[Net] public GridManager GridManager { get; private set; }
		[Net] public PersonManager PersonManager { get; private set; }
		[Net] public StructureManager StructureManager { get; private set; }

		public MinimalHudEntity Hud;

		private readonly List<FloaterText> _floaterTexts = new();

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

			if ( IsClient )
			{
				for ( int i = _floaterTexts.Count - 1; i >= 0; i-- )
				{
					var floater = _floaterTexts[i];
					floater.Update( Time.Delta );

					if(floater.ShouldRemove)
						_floaterTexts.Remove( floater );
				}
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

		[ClientRpc]
		public void SpawnFloater( Vector3 pos, string text, Color color )
		{
			_floaterTexts.Add( new FloaterText( pos, text, color ) );
		}

		public void MakeNoise( Vector2 noisePos, float loudness, PersonType noiseType )
		{
			Host.AssertServer();

			var people = Entity.All.OfType<Person>()
					.Where( p => !p.IsDead )
					.Where( p => !p.IsSpawning )
					.Where( p => p.PersonType != PersonType.None && p.PersonType != noiseType )
					.ToList();

			foreach ( var person in people )
			{
				float sqrDistToNoise = (noisePos - person.Position2D).LengthSquared;
				float sqrHearingRadius = MathF.Pow( person.HearingRadius, 2f );

				if ( sqrDistToNoise < sqrHearingRadius )
				{
					float distFactor = sqrDistToNoise / sqrHearingRadius;
					if ( Rand.Float( 0f, 1f ) * (1f - distFactor) > (1f - loudness) )
					{
						person.HeardNoise( noisePos );
					}
				}
			}

			// DebugOverlay.Line( new Vector3( noisePos.x, noisePos.y, 1f ), new Vector3( noisePos.x, noisePos.y, Utils.Map( loudness, 0f, 1f, 10f, 200f )), Color.Red, 1f, true );
		}

		public Scrap CreateScrap( Vector3 pos, int amount )
		{
			Scrap scrap = new Scrap { Position = pos};
			scrap.SetPosition2D( new Vector2( pos.x, pos.y ) );
			scrap.Init( amount );
			scrap.Drop( Utils.GetVector2FromAngleDegrees( Rand.Float( 0f, 360f ) ), Rand.Float( 50f, 400f ), Rand.Float( 3f, 100f ), 6 );

			return scrap;
		}

		public AmmoItem CreateAmmoItem( Vector3 posA, Vector3 posB, AmmoType ammoType, int amount, float peakHeight, float airTimeTotal, int numFlips )
		{
			AmmoItem ammoItem = new AmmoItem { Position = posA };
			ammoItem.SetPosition2D( new Vector2( posA.x, posA.y ) );
			ammoItem.Init( ammoType, amount );
			ammoItem.PlaceItem( posB, peakHeight, airTimeTotal, numFlips );
			return ammoItem;
		}
	}
}
