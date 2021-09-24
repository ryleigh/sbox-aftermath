using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class BuildCommand : PersonCommand
	{
		public GridPosition GridPos { get; private set; }
		public StructureType StructureType { get; private set; }
		public Direction StructureDirection { get; private set; }

		public Structure Structure { get; private set; }

		private float _buildTimer;
		public float BuildTime { get; private set; }

		private readonly int _cost;

		public override string ToString() { return $"Build: {StructureType}"; }

		public float Progress { get; private set; }

		public BuildCommand( GridPosition gridPos, StructureType structureType, Direction structureDirection, int cost )
		{
			Type = PersonCommandType.Build;
			StructureType = structureType;
			StructureDirection = structureDirection;
			GridPos = gridPos;
			BuildTime = Structure.GetBuildTime( structureType );
			_cost = cost;
		}

		public override void Begin()
		{
			base.Begin();

			if ( !GridPos.IsValid || Person == null )
			{
				Finish();
				return;
			}

			if ( Person.Player.ScrapAmount < _cost )
			{
				AftermathGame.Instance.SpawnFloater( Person.Position, $"TOO POOR!", new Color( 1f, 0.2f, 1f, 1f ) );
				Finish();
				return;
			}

			AftermathGame.Instance.SpawnFloater( Person.Position, $"SPENT {_cost}!", new Color( 1f, 0.2f, 1f, 1f ) );
			Person.Player.AdjustScrapAmount( -_cost );

			Structure = AftermathGame.Instance.StructureManager.AddStructureServer( GridPos, StructureType, StructureDirection );
			if ( Structure == null )
			{
				Finish();
				return;
			}
			Structure.IsBeingBuilt = true;
		}

		public override void Update( float dt )
		{
			if ( IsFinished )
				return;

			base.Update( dt );

			Progress = Utils.Map( _buildTimer, 0f, BuildTime, 0f, 1f );

			// if(Rand.Float( 0f, 1f) < 0.1f )
			// 	AftermathGame.Instance.SpawnFloater( Structure.Position + new Vector3( 0f, 10f, 2f ), $"{(Progress * 100f).FloorToInt()}", new Color( 1f, 1f, 1f, 1f ) );

			GridManager grid = AftermathGame.Instance.GridManager;

			_buildTimer += dt * Person.BuildSpeedFactor;

			if ( _buildTimer >= BuildTime )
			{
				Structure.Position = grid.GetWorldPosForGridPos( GridPos ) ;
				Finish();
			}
			else
			{
				float heightProgress = Utils.Map( _buildTimer, 0f, BuildTime, 0f, 1f, EasingType.SineOut );
				float height = (1f - heightProgress) * -grid.SquareSize;
				Structure.Position = grid.GetWorldPosForGridPos( GridPos ) + new Vector3( 0f, 0f, height );
			}
		}

		public override void Finish()
		{
			base.Finish();

			// if ( _structureBuilding != null )
			// {
			// 	_structureBuilding.Remove();
			// }

			if(Structure != null)
				Structure.IsBeingBuilt = false;
			// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.None );
		}

		public override void Interrupt()
		{
			base.Interrupt();

			// if ( _structureBuilding != null )
			// {
			// 	_structureBuilding.Remove();
			// }

			if ( Structure != null )
				AftermathGame.Instance.StructureManager.RemoveStructure( Structure );

			// int refundAmount = ((1f - Progress) * _cost).FloorToInt();
			// if ( refundAmount > 0 )
			// {
			// 	AftermathGame.Instance.SpawnFloater( Person.Position, $"REFUNDED {refundAmount}!", new Color( 0.4f, 0.2f, 1f, 1f ) );
			// 	Person.Player.AdjustScrapAmount( refundAmount );
			// }

			// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.None );
			// Person.Sounds.Play( Plugin.GetResource<SoundEffect>( "Zombies.Structure.WallDestroyed" ) );
		}
	}
}
