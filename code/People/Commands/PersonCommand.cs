using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public partial class PersonCommand : ICommandHandler
	{
		public PersonCommandType Type { get; set; }
		public int CommandNumber { get; set; }

		public Person Person { get; set; }

		public bool HasStarted { get; set; }
		public bool IsFinished { get; set; }
		public bool IsPaused { get; set; }

		public ICommandHandler CurrentHandler { get; set; }

		public virtual void Init( Person person, ICommandHandler handler )
		{
			Person = person;
			CurrentHandler = handler;
		}

		public virtual void Begin()
		{
			//            if(Person.IsSelected)
			//                Debug.Log("- Begin: " + ToString() + ((CurrentHandler is Person_CommandHandler) ? "# commands: " + ((Person_CommandHandler)CurrentHandler).CommandList.Count : "") );

			HasStarted = true;
		}

		public virtual void Update( float dt )
		{

		}

		public virtual void Finish()
		{
			IsFinished = true;
			CurrentHandler.FinishCommand( this );

			//            if (Person.IsSelected)
			//                Debug.Log("- Finish: " + ToString() + ((CurrentHandler is Person_CommandHandler) ? "# commands: " + ((Person_CommandHandler)CurrentHandler).CommandList.Count : ""));
		}

		public virtual void Interrupt()
		{
			IsFinished = true;
		}

		public virtual void Pause()
		{
			IsPaused = true;
		}

		public virtual void Resume()
		{
			IsPaused = false;
		}

		// used in ParallelCommand when a subcommand finishes
		public virtual void FinishCommand( PersonCommand command )
		{

		}
	}
}
