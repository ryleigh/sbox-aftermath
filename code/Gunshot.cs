using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public enum GunshotImpactType { None, Ground, Wood, Metal, Flesh }

	public partial class Gunshot : ModelEntity
	{
		[Net] public AmmoType AmmoType { get; private set; }

		[Net] public Vector3 Start { get; set; }
		[Net] public Vector3 Direction { get; set; }
		[Net] public float Length { get; set; }
		[Net] public float CurrentLength { get; set; }

		[Net] public Vector3 PosA { get; set; }
		[Net] public Vector3 PosB { get; set; }

		public float Speed { get; private set; }
		public float Damage { get; set; }
		public float BulletForce { get; set; }
		public float PenetrationChance { get; private set; }

		public bool HasHit { get; set; }

		public Person ShootingPerson { get; private set; }
		public Structure ShootingStructure { get; private set; }
		public List<Person> HitPeople = new();

		public bool IsActive { get; set; }
		public Vector2 Position2D { get; private set; }

		private Color _color;

		public Gunshot()
		{
			Transmit = TransmitType.Always;
			IsActive = false;
		}


		public void Init(Vector3 pos, Vector3 dir, float length, float speed, float damage, float bulletForce, float penetrationChance, Person shootingPerson, Structure shootingStructure, AmmoType ammoType)
		{
			AmmoType = ammoType;

			switch ( ammoType )
			{
				case AmmoType.Bullet: _color = new Color( 1f, 1f, 0f ); break;
				case AmmoType.Shell: _color = new Color( 1f, 0.8f, 0f ); break;
				case AmmoType.HPBullet: _color = new Color( 0.8f, 0.8f, 1f ); break;
				case AmmoType.Grenade: _color = new Color( 0.7f, 0.7f, 0.6f ); break;
				default: _color = new Color( 1f, 1f, 1f ); break;
			}

			Start = pos;
			Direction = dir;
			Length = length;
			CurrentLength = length;

			PosA = pos;
			PosB = pos;
			Position2D = Utils.GetVector2( pos );

			Speed = speed;
			Damage = damage;
			BulletForce = bulletForce;
			PenetrationChance = penetrationChance;

			ShootingPerson = shootingPerson;
			ShootingStructure = shootingStructure;

			IsActive = true;
			HasHit = false;
		}

		public override void Spawn()
		{
			base.Spawn();

		}

		[Event.Tick.Server]
		protected virtual void Tick()
		{
			if(!IsActive)
				return;

			float dt = Time.Delta;

			DebugOverlay.Line( PosA, PosB, _color, 0f, true );

			CheckBounds();

			if ( !HasHit )
			{
				PosB += Direction * Speed * dt;
				Position2D = Utils.GetVector2( PosA );
				CurrentLength = (PosB - PosA).Length;

				if ( CurrentLength > Length )
					PosA += Direction * Speed * dt;

				CheckCollision();
			}
			else
			{
				PosA += Direction * Speed * dt;

				float distanceA = (PosA - Start).LengthSquared;
				float distanceB = (PosB - Start).LengthSquared;

				if ( distanceA > distanceB )
					Remove();
			}
		}

		[Event.Tick.Client]
		protected virtual void ClientTick()
		{
			if ( !IsActive )
				return;

			float dt = Time.Delta;

			DebugOverlay.Line( PosA, PosB, _color, 0f, true );
		}

		private void CheckBounds()
		{
			if ( PosB.LengthSquared > 2000f * 2000f )
				HasHit = true;
		}

		private bool CheckCollision()
		{
			bool didHitPerson = false;
			Person hitPerson = null;
			Vector3 hitPosPerson = Vector3.Zero;
			Vector3 hitNormalPerson = Vector3.Zero;

			bool didHitStructure = false;
			Structure hitStructure = null;
			Vector3 hitPosStructure = Vector3.Zero;
			Vector3 hitNormalStructure = Vector3.Zero;

			GridManager gridManager = AftermathGame.Instance.GridManager;

			if ( gridManager.Raycast( PosA, PosB, RaycastMode.Gunshot, out var gridPos, out var hitPos, out var normal ) )
			{
				hitStructure = AftermathGame.Instance.StructureManager.GetStructure( gridPos );
				if ( hitStructure != null )
				{
					// get z-value of collision
					Plane plane = new Plane( hitPos, new Vector3( normal.x, normal.y, 0f ) );
					Vector3? planeHitPos = plane.Trace( new Ray( PosA, Direction), true, Length);
					if ( planeHitPos != null )
					{
						// check if gunshot flew above structure or not
						if ( planeHitPos.Value.z < hitStructure.Height )
						{
							didHitStructure = true;
							hitPosStructure = planeHitPos.Value;
							hitNormalStructure = new Vector3( -normal.x, normal.y, 0f );
						}
					}
				}

				// AftermathGame.Instance.StructureManager.GetStructure( gridPos )?.Delete();
				// return true;
			}

			var trace = Trace.Ray(PosA, PosB).EntitiesOnly().WithTag( "person" ).Run();
			if ( trace.Hit )
			{
				if ( trace.Entity is Person p )
				{
					didHitPerson = true;
					hitPerson = p;
					hitPosPerson = trace.EndPos;
					hitNormalPerson = trace.Normal;
				}
			}

			if ( didHitPerson && didHitStructure )
			{
				// check which hit was closest
				if ( (hitPosPerson - PosA).LengthSquared < (hitPosStructure - PosA).LengthSquared )
				{
					bool shouldStop = PersonCollision( hitPerson, hitPosPerson, hitNormalPerson );
					if(shouldStop)
						Hit( hitPosPerson );
				}
				else
				{
					StructureCollision( hitStructure, hitPosStructure, hitNormalStructure );
					Hit( hitPosStructure );
				}
			}
			else if ( didHitPerson )
			{
				bool shouldStop = PersonCollision( hitPerson, hitPosPerson, hitNormalPerson );
				if ( shouldStop )
					Hit( hitPosPerson );
			} 
			else if ( didHitStructure )
			{
				StructureCollision( hitStructure, hitPosStructure, hitNormalStructure );
				Hit( hitPosStructure );
			}

			return false;
		}

		private bool PersonCollision(Person hitPerson, Vector3 hitPos, Vector3 normal)
		{
			bool penetrate = Rand.Float( 0f, 1f ) < PenetrationChance;

			hitPerson.HitByGunshot( this, hitPos, penetrate );

			return !penetrate;
		}

		private bool StructureCollision( Structure hitStructure, Vector3 hitPos, Vector3 normal )
		{
			hitStructure.HitByGunshot( this, hitPos );

			return true;
		}

		public void Hit( Vector3 hitPoint )
		{
			if ( !HasHit )
			{
				HasHit = true;

				PosB = hitPoint;
			}
		}

		private void Impact( Vector3 hitPoint, Vector3 debrisDirection, GunshotImpactType impactType )
		{

		}

		public void Remove()
		{
			Delete();
		}
	}
}
