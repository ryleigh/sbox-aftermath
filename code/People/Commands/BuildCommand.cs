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

		public override string ToString() { return $"Build: {StructureType}"; }

		public BuildCommand( GridPosition gridPos, StructureType structureType, Direction structureDirection )
		{
			Type = PersonCommandType.Build;
			StructureType = structureType;
			StructureDirection = structureDirection;
			GridPos = gridPos;
			BuildTime = Structure.GetBuildTime( structureType );
		}

		public override void Begin()
		{
			base.Begin();

			if ( !GridPos.IsValid || Person == null )
			{
				Finish();
				return;
			}

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

			GridManager grid = AftermathGame.Instance.GridManager;

			_buildTimer += dt * Person.BuildSpeedFactor;

			if ( _buildTimer >= BuildTime )
			{
				Structure.Position = grid.GetWorldPosForGridPos( GridPos ) ;
				Finish();
			}
			else
			{
				float progress = Utils.Map( _buildTimer, 0f, BuildTime, 0f, 1f, EasingType.SineOut );
				float height = (1f - progress) * -grid.SquareSize;
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

			AftermathGame.Instance.StructureManager.RemoveStructure( Structure );

			// Person.BodyAnimHandler.SetAnim( PersonAnimationMode.None );
			// Person.Sounds.Play( Plugin.GetResource<SoundEffect>( "Zombies.Structure.WallDestroyed" ) );
		}
	}
}
