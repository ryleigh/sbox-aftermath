using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class SimpleTerryAnimator : UnitAnimator
	{
		private struct AnimationHoldType
		{
			public string Idle;
			public string Attack;
			public string Run;
			public string Walk;

			public AnimationHoldType( string idle, string attack, string walk, string run )
			{
				Idle = idle;
				Attack = attack;
				Walk = walk;
				Run = run;
			}
		}

		private static AnimationHoldType[] Lookup = new AnimationHoldType[]
		{
				new AnimationHoldType( "Idle", "Idle", "Walk", "Run" ),
				new AnimationHoldType( "Idle_Pistol", "Attack_Pistol", "Walk_Pistol", "Run_Pistol" ),
				new AnimationHoldType( "Idle_SMG", "Attack_SMG", "Walk_SMG", "Run_SMG" ),
				new AnimationHoldType( "Idle_Shotgun", "Attack_Shotgun", "Walk_Shotgun", "Run_Shotgun" ),
				new AnimationHoldType( "Idle_Sniper", "Attack_Sniper", "Walk_Sniper", "Run_Sniper" ),
				new AnimationHoldType( "Idle_RPG", "Attack_RPG", "Walk_RPG", "Run_RPG" )
		};

		public override void Apply( Person person )
		{
			if ( !person.Attacking )
			{
				if ( person.Speed >= 0.5f )
					person.CurrentSequence.Name = Lookup[person.HoldType].Run;
				else if ( person.Speed > 0f )
					person.CurrentSequence.Name = Lookup[person.HoldType].Walk;
				else
					person.CurrentSequence.Name = Lookup[person.HoldType].Idle;
			}
			else
			{
				person.CurrentSequence.Name = Lookup[person.HoldType].Attack;
			}
		}
	}

	public class UnitAnimator
	{
		public virtual void Reset()
		{
			
		}

		public virtual void Apply( Person person )
		{

		}
	}
}
