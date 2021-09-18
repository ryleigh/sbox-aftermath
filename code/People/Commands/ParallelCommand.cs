using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class ParallelCommand : PersonCommand
	{
		public List<PersonCommand> SubCommands { get; private set; }

		public bool ReqAllFinished { get; set; }

		public override string ToString()
		{
			return "ParallelCommand: (" + SubCommands.Count + ") " + SubCommands.Aggregate( "", ( current, subCommand ) => current + ("\n- " + subCommand.ToString() + ": " + subCommand.IsFinished) );
		}

		public ParallelCommand( List<PersonCommand> subCommands )
		{
			foreach ( var command in subCommands )
				command.CurrentHandler = this;

			SubCommands = subCommands;
			Type = PersonCommandType.Parallel;
		}

		public override void Init( Person person, ICommandHandler handler )
		{
			base.Init( person, handler );

			foreach ( var command in SubCommands )
				command.Init( person, this );
		}

		public override void Begin()
		{
			base.Begin();

			foreach ( var command in SubCommands )
				command.Begin();
		}

		public override void Update( float dt )
		{
			base.Update( dt );

			foreach ( var command in SubCommands )
				command.Update( dt );
		}

		public override void FinishCommand( PersonCommand command )
		{
			bool finished = true;

			if ( ReqAllFinished )
			{
				foreach ( var subCommand in SubCommands )
				{
					if ( !subCommand.IsFinished )
					{
						finished = false;
						break;
					}
				}
			}

			if(finished)
				Finish();
		}

		public override void Finish()
		{
			// Log.Info( "ParallelCommand - Finish" );

			foreach ( var command in SubCommands )
			{
				if(!command.IsFinished)
					command.Interrupt();
			}

			base.Finish();
		}

		public override void Interrupt()
		{
			// Log.Info( "ParallelCommand - Interrupt" );

			foreach ( var command in SubCommands )
			{
				if(!command.IsFinished)
					command.Interrupt();
			}

			base.Interrupt();
		}
	}
}
