using System.Collections.Generic;
using System.Windows.Input;
using Sandbox;

namespace aftermath
{
	public enum PersonCommandType
	{
		None, Parallel, MoveToPos, LookForTarget, FollowTarget, AimAtTarget, Shoot, Bite, MoveToPickUpItem, PickUpItem, Reload,
		MoveToBuild, Build, MoveToAttackStructure, MoveToInteractWithStructure, PlaceItem, Rescuable, MoveAndLook, Wait
	}

	public interface ICommandHandler
	{
		void FinishCommand( PersonCommand command );
	}

	public delegate void PersonCommandDelegate( Person_CommandHandler commandHandler );

	public partial class Person_CommandHandler : PersonComponent, ICommandHandler
	{
		public List<PersonCommand> CommandList = new();
		public PersonCommand CurrentCommand => CommandList.Count > 0 ? CommandList[0] : null;
		public PersonCommandType CurrentCommandType => CommandList.Count > 0 ? CommandList[0].Type : PersonCommandType.None;

		public event PersonCommandDelegate StartCommand;
		public event PersonCommandDelegate FinishedCommand;
		public event PersonCommandDelegate FinishedAllCommands;

		public Person_CommandHandler()
		{

		}

		public override void Update( float dt )
		{
			if ( Person.IsDead )
				return;

			CurrentCommand?.Update( dt );
		}

		public void AddCommand( PersonCommand command )
		{
			command.Init( Person, this );

			CommandList.Add( command );
			RefreshCommandNumbers();

			if ( CommandList.Count == 1 )
				BeginCommand( command );
		}

		public void InsertCommand( PersonCommand command, int index = 0 )
		{
			command.Init( Person, this );

			if ( CurrentCommand != null && index <= CurrentCommand.CommandNumber )
			{
				CurrentCommand.Pause();
			}

			CommandList.Insert( index, command );
			RefreshCommandNumbers();

			if ( index == 0 )
				BeginCommand( command );
		}

		public void SetCommand( PersonCommand command )
		{
			ClearCommands();
			AddCommand( command );
			RefreshCommandNumbers();
		}

		public void ClearCommands()
		{
			CurrentCommand?.Interrupt();

			CommandList.Clear();
		}

		public PersonCommand GetCurrentCommandIfExists( PersonCommandType commandType )
		{
			if ( CurrentCommandType == commandType )
				return CurrentCommand;

			// if ( CurrentCommandType == PersonCommandType.Parallel )
			// {
			// 	ParallelCommand parallelCommand = (ParallelCommand)CurrentCommand;
			//
			// 	foreach ( PersonCommand personCommand in parallelCommand.SubCommands )
			// 	{
			// 		if ( personCommand.Type == commandType )
			// 		{
			// 			return personCommand;
			// 		}
			// 	}
			// }

			return null;
		}

		public void RefreshCommandNumbers()
		{
			for ( int i = 0; i < CommandList.Count; i++ )
			{
				CommandList[i].CommandNumber = i;
			}

			// if ( Person.IsExclusivelySelected )
			// 	GameMode.PersonManager.RefreshPersonInfo( Person );
		}

		void BeginCommand( PersonCommand command )
		{
			command.Begin();
			StartCommand?.Invoke( this );
		}

		public void FinishCommand( PersonCommand command )
		{
			if ( !CommandList.Contains( command ) )
				return;

			bool wasCurrentCommand = (command == CommandList[0]);

			CommandList.Remove( command );

			FinishedCommand?.Invoke( this );

			if ( wasCurrentCommand )
			{
				if ( CommandList.Count > 0 )
				{
					if ( !CommandList[0].HasStarted )
					{
						BeginCommand( CommandList[0] );
					}
					else
					{
						CommandList[0].Resume();
					}
				}
				else
				{
					FinishedAllCommands?.Invoke( this );
				}
			}
		}
	}
}
