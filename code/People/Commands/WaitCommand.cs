using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public delegate void WaitFinishedDelegate( WaitCommand waitCommand );

	public class WaitCommand : PersonCommand
	{
		private float _waitTime;

		public event WaitFinishedDelegate WaitFinished;

		public override string ToString() { return $"Wait: {_waitTime}"; }

		public WaitCommand( float waitTime )
		{
			_waitTime = waitTime;
			Type = PersonCommandType.Wait;
		}

		public override void Update( float dt )
		{
			if( IsFinished ) return;

			_waitTime -= dt;
			if(_waitTime <= 0f)
				Finish();
		}

		public override void Finish()
		{
			base.Finish();

			WaitFinished?.Invoke( this );
		}
	}
}
