using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace aftermath
{
	public partial class PersonManager : Entity
	{
		public PersonManager()
		{
			Transmit = TransmitType.Always;
		}

		public void Initialize()
		{

		}

		public void SpawnPersonServer( Vector2 worldPos, Player player, PersonType personType)
		{
			Host.AssertServer();

			Person person = null;
			if ( personType == PersonType.Survivor )
				person = new Survivor();
			else if ( personType == PersonType.Zombie )
				person = new Zombie();

			if(person == null)
				return;

			person.Assign( player );
			person.SetPosition2D( worldPos );

			person.Tags.Add( "selectable" );
			person.Tags.Add( "person" );

			Log.Warning( $"PersonManager - SpawnPersonServer - person: {person}, player: {player}, NetworkIdent: {player.GetClientOwner().NetworkIdent}, IsServer: {IsServer}" );
		}
	}
}
