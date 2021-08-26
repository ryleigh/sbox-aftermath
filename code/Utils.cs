using System;
using System.Collections.Generic;
using Sandbox;

namespace aftermath
{
	public enum EasingType
	{
		None = -1,
		Linear = 0,
		SineIn, SineOut, SineInOut,
		QuadIn, QuadOut, QuadInOut,
		CubicIn, CubicOut, CubicInOut,
		QuartIn, QuartOut, QuartInOut,
		QuintIn, QuintOut, QuintInOut,
		ExpoIn, ExpoOut, ExpoInOut,
		ExtremeIn, ExtremeOut, ExtremeInOut,
		ElasticIn, ElasticOut, ElasticInOut,
		ElasticSoftIn, ElasticSoftOut, ElasticSoftInOut,
		BackIn, BackOut, BackInOut,
		BounceIn, BounceOut, BounceInOut
	};

	public struct AStarEdge<T>
	{
		/// <summary>
		/// The node this connection leads to.
		/// </summary>
		public readonly T Dest;

		/// <summary>
		/// The cost of using this connection.
		/// </summary>
		public readonly float Cost;

		internal AStarEdge( T dest, float cost )
		{
			Dest = dest;
			Cost = cost;
		}
	}

	public static class Utils
	{
		public static float EaseUnclamped( float value, EasingType easingType )
		{
			switch ( easingType )
			{
				case EasingType.SineIn: return SineIn( value );
				case EasingType.SineOut: return SineOut( value );
				case EasingType.SineInOut: return SineInOut( value );

				case EasingType.QuadIn: return QuadIn( value );
				case EasingType.QuadOut: return QuadOut( value );
				case EasingType.QuadInOut: return QuadInOut( value );

				case EasingType.CubicIn: return CubicIn( value );
				case EasingType.CubicOut: return CubicOut( value );
				case EasingType.CubicInOut: return CubicInOut( value );

				case EasingType.QuartIn: return QuartIn( value );
				case EasingType.QuartOut: return QuartOut( value );
				case EasingType.QuartInOut: return QuartInOut( value );

				case EasingType.QuintIn: return QuintIn( value );
				case EasingType.QuintOut: return QuintOut( value );
				case EasingType.QuintInOut: return QuintInOut( value );

				case EasingType.ExpoIn: return ExpoIn( value );
				case EasingType.ExpoOut: return ExpoOut( value );
				case EasingType.ExpoInOut: return ExpoInOut( value );

				case EasingType.ExtremeIn: return ExtremeIn( value );
				case EasingType.ExtremeOut: return ExtremeOut( value );
				case EasingType.ExtremeInOut: return ExtremeInOut( value );

				case EasingType.ElasticIn: return ElasticIn( value );
				case EasingType.ElasticOut: return ElasticOut( value );
				case EasingType.ElasticInOut: return ElasticInOut( value );

				case EasingType.ElasticSoftIn: return ElasticSoftIn( value );
				case EasingType.ElasticSoftOut: return ElasticSoftOut( value );
				case EasingType.ElasticSoftInOut: return ElasticSoftInOut( value );

				case EasingType.BackIn: return BackIn( value );
				case EasingType.BackOut: return BackOut( value );
				case EasingType.BackInOut: return BackInOut( value );

				case EasingType.BounceIn: return BounceIn( value );
				case EasingType.BounceOut: return BounceOut( value );
				case EasingType.BounceInOut: return BounceInOut( value );
				default: return value;
			}
		}

		public static float Map( float value, float inputMin, float inputMax, float outputMin, float outputMax, EasingType easingType = EasingType.Linear, bool clamp = true )
		{
			if ( inputMin.Equals( inputMax ) || outputMin.Equals( outputMax ) )
				return outputMin;

			//            if (inputMin.Equals(inputMax) || outputMin.Equals(outputMax))
			//                return outputMax;

			if ( clamp )
			{
				// clamp input
				if ( inputMax > inputMin )
				{
					if ( value < inputMin ) value = inputMin;
					else if ( value > inputMax ) value = inputMax;
				}
				else if ( inputMax < inputMin )
				{
					if ( value > inputMin ) value = inputMin;
					else if ( value < inputMax ) value = inputMax;
				}
			}

			var ratio = EaseUnclamped( (value - inputMin) / (inputMax - inputMin), easingType );
			var outVal = outputMin + ratio * (outputMax - outputMin);
			return outVal;
		}

		public static float MapReturn( float value, float inputMin, float inputMax, float outputMin, float outputMax, EasingType easingType )
		{
			var halfway = inputMin + (inputMax - inputMin) * 0.5f;
			if ( value < halfway ) return Map( value, inputMin, halfway, outputMin, outputMax, easingType );
			else return Map( value, halfway, inputMax, outputMax, outputMin, GetOppositeEasingType( easingType ) );
		}

		public static EasingType GetOppositeEasingType( EasingType easingType )
		{
			var opposite = EasingType.Linear;
			switch ( easingType )
			{
				case EasingType.SineIn: opposite = EasingType.SineOut; break;
				case EasingType.SineOut: opposite = EasingType.SineIn; break;
				case EasingType.SineInOut: opposite = EasingType.SineInOut; break;

				case EasingType.QuadIn: opposite = EasingType.QuadOut; break;
				case EasingType.QuadOut: opposite = EasingType.QuadIn; break;
				case EasingType.QuadInOut: opposite = EasingType.QuadInOut; break;

				case EasingType.CubicIn: opposite = EasingType.CubicOut; break;
				case EasingType.CubicOut: opposite = EasingType.CubicIn; break;
				case EasingType.CubicInOut: opposite = EasingType.CubicInOut; break;

				case EasingType.QuartIn: opposite = EasingType.QuartOut; break;
				case EasingType.QuartOut: opposite = EasingType.QuartIn; break;
				case EasingType.QuartInOut: opposite = EasingType.QuartInOut; break;

				case EasingType.QuintIn: opposite = EasingType.QuintOut; break;
				case EasingType.QuintOut: opposite = EasingType.QuintIn; break;
				case EasingType.QuintInOut: opposite = EasingType.QuintInOut; break;

				case EasingType.ExpoIn: opposite = EasingType.ExpoOut; break;
				case EasingType.ExpoOut: opposite = EasingType.ExpoIn; ; break;
				case EasingType.ExpoInOut: opposite = EasingType.ExpoInOut; break;

				case EasingType.ExtremeIn: opposite = EasingType.ExtremeOut; break;
				case EasingType.ExtremeOut: opposite = EasingType.ExtremeIn; break;
				case EasingType.ExtremeInOut: opposite = EasingType.ExtremeInOut; break;

				case EasingType.ElasticIn: opposite = EasingType.ElasticOut; break;
				case EasingType.ElasticOut: opposite = EasingType.ElasticIn; break;
				case EasingType.ElasticInOut: opposite = EasingType.ElasticInOut; break;

				case EasingType.ElasticSoftIn: opposite = EasingType.ElasticSoftOut; break;
				case EasingType.ElasticSoftOut: opposite = EasingType.ElasticSoftIn; break;
				case EasingType.ElasticSoftInOut: opposite = EasingType.ElasticSoftInOut; break;

				case EasingType.BackIn: opposite = EasingType.BackOut; break;
				case EasingType.BackOut: opposite = EasingType.BackIn; break;
				case EasingType.BackInOut: opposite = EasingType.BackInOut; break;

				case EasingType.BounceIn: opposite = EasingType.BounceOut; break;
				case EasingType.BounceOut: opposite = EasingType.BounceIn; break;
				case EasingType.BounceInOut: opposite = EasingType.BounceInOut; break;
			}

			return opposite;
		}

		public static float SineIn( float t ) { return 1f - (float)Math.Cos( t * Math.PI * 0.5f ); }
		public static float SineOut( float t ) { return (float)Math.Sin( t * (Math.PI * 0.5f) ); }
		public static float SineInOut( float t ) { return -0.5f * ((float)Math.Cos( Math.PI * t ) - 1f); }

		public static float QuadIn( float t ) { return t * t; }
		public static float QuadOut( float t ) { return t * (2f - t); }
		public static float QuadInOut( float t ) { return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t; }

		public static float CubicIn( float t ) { return t * t * t; }
		public static float CubicOut( float t ) { var t1 = t - 1f; return t1 * t1 * t1 + 1f; }
		public static float CubicInOut( float t ) { return t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f; }

		public static float QuartIn( float t ) { return t * t * t * t; }
		public static float QuartOut( float t ) { var t1 = t - 1f; return 1f - t1 * t1 * t1 * t1; }
		public static float QuartInOut( float t ) { return t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f; }

		public static float QuintIn( float t ) { return t * t * t * t * t; }
		public static float QuintOut( float t ) { var t1 = t - 1f; return 1f + t1 * t1 * t1 * t1 * t1; }
		public static float QuintInOut( float t ) { var t1 = t - 1f; return t < 0.5f ? 16f * t * t * t * t * t : 1f + 16f * t1 * t1 * t1 * t1 * t1; }

		public static float ExpoIn( float t ) { return (float)Math.Pow( 2f, 10f * (t - 1f) ); }
		public static float ExpoOut( float t ) { return 1f - (float)Math.Pow( 2f, -10f * t ); }
		public static float ExpoInOut( float t ) { return t < 0.5f ? ExpoIn( t * 2f ) * 0.5f : 1f - ExpoIn( 2f - t * 2f ) * 0.5f; }

		public static float ExtremeIn( float t ) { return (float)Math.Pow( 10f, 10f * (t - 1f) ); }
		public static float ExtremeOut( float t ) { return 1f - (float)Math.Pow( 10f, -10f * t ); }
		public static float ExtremeInOut( float t ) { return t < 0.5f ? ExtremeIn( t * 2f ) * 0.5f : 1f - ExtremeIn( 2f - t * 2f ) * 0.5f; }

		public static float ElasticIn( float t ) { return 1f - ElasticOut( 1f - t ); }
		public static float ElasticOut( float t ) { var p = 0.3f; return (float)Math.Pow( 2f, -10f * t ) * (float)Math.Sin( (t - p / 4f) * (2f * (float)Math.PI) / p ) + 1f; }
		public static float ElasticInOut( float t ) { return t < 0.5f ? ElasticIn( t * 2f ) * 0.5f : 1f - ElasticIn( 2f - t * 2f ) * 0.5f; }

		public static float ElasticSoftIn( float t ) { return 1f - ElasticSoftOut( 1f - t ); }
		public static float ElasticSoftOut( float t ) { var p = 0.5f; return (float)Math.Pow( 2f, -10f * t ) * (float)Math.Sin( (t - p / 4f) * (2f * (float)Math.PI) / p ) + 1f; }
		public static float ElasticSoftInOut( float t ) { return t < 0.5f ? ElasticSoftIn( t * 2f ) * 0.5f : 1f - ElasticSoftIn( 2f - t * 2f ) * 0.5f; }

		public static float BackIn( float t ) { var p = 1f; return t * t * ((p + 1f) * t - p); }
		public static float BackOut( float t ) { var p = 1f; var scaledTime = t / 1f - 1f; return scaledTime * scaledTime * ((p + 1f) * scaledTime + p) + 1f; }
		public static float BackInOut( float t )
		{
			var p = 1f;
			var scaledTime = t * 2f;
			var scaledTime2 = scaledTime - 2f;
			var s = p * 1.525f;

			if ( scaledTime < 1f ) return 0.5f * scaledTime * scaledTime * ((s + 1f) * scaledTime - s);
			else return 0.5f * (scaledTime2 * scaledTime2 * ((s + 1f) * scaledTime2 + s) + 2f);
		}

		public static float BounceIn( float t ) { return 1f - BounceOut( 1f - t ); }
		public static float BounceOut( float t )
		{
			var scaledTime = t / 1f;

			if ( scaledTime < 1 / 2.75f )
			{
				return 7.5625f * scaledTime * scaledTime;
			}
			else if ( scaledTime < 2 / 2.75 )
			{
				var scaledTime2 = scaledTime - 1.5f / 2.75f;
				return 7.5625f * scaledTime2 * scaledTime2 + 0.75f;
			}
			else if ( scaledTime < 2.5 / 2.75 )
			{
				var scaledTime2 = scaledTime - 2.25f / 2.75f;
				return 7.5625f * scaledTime2 * scaledTime2 + 0.9375f;
			}
			else
			{
				var scaledTime2 = scaledTime - 2.625f / 2.75f;
				return 7.5625f * scaledTime2 * scaledTime2 + 0.984375f;
			}
		}
		public static float BounceInOut( float t )
		{
			if ( t < 0.5 ) return BounceIn( t * 2f ) * 0.5f;
			else return BounceOut( t * 2f - 1f ) * 0.5f + 0.5f;
		}

		public static float Deg2Rad( float angle ) { return (float)Math.PI * angle / 180.0f; }
		public static float Rad2Deg( float angle ) { return angle * (180.0f / (float)Math.PI); }

		public static float GetAngleDegreesFromVector( Vector2 vector )
		{
			return Rad2Deg( (float)(Math.Atan2( vector.x, vector.y ) - Math.Atan2( 1.0f, 0.0f )) );
		}

		public static Vector2 GetVector2FromAngle( float radians )
		{
			return new Vector2( MathF.Cos( radians ), MathF.Sin( -radians ) );
		}

		public static Vector2 GetVector2FromAngleDegrees( float degrees)
		{
			return GetVector2FromAngle( Deg2Rad( degrees ) );
		}

		public static Rotation RotateQuaternion( Rotation quaternion, Vector3 rot )
		{
			Angles angles = quaternion.Angles();
			Vector3 euler = new Vector3( angles.yaw, angles.pitch, angles.roll );
			Vector3 rotated = RotateVectorRandomly( euler, rot );
			return Rotation.From( new Angles( rotated.x, rotated.y, rotated.z ));
		}

		public static Vector3 RotateVectorRandomly( Vector3 vec, float amount )
		{
			return new Vector3(
				vec.x + Rand.Float( -amount / 2f, amount / 2f ),
				vec.y + Rand.Float( -amount / 2f, amount / 2f ),
				vec.z + Rand.Float( -amount / 2f, amount / 2f )
			);
		}

		public static Vector3 RotateVectorRandomly( Vector3 vec, Vector3 rot )
		{
			return new Vector3(
				vec.x + Rand.Float( -rot.x / 2f, rot.x / 2f ),
				vec.y + Rand.Float( -rot.y / 2f, rot.y / 2f ),
				vec.z + Rand.Float( -rot.z / 2f, rot.z / 2f )
			);
		}

		public static Vector2 RotateVector2( Vector2 vec, float degrees )
		{
			float rads = vec.AngleRadians() + Deg2Rad( degrees );
			return new Vector2( MathF.Cos( rads ), MathF.Sin( rads ) );
		}

		public static float Lerp( float a, float b, float t )
		{
			return a + (b - a) * t;
		}

		public static Vector2 GetVector2( Vector3 vector )
		{
			return new Vector2( vector.x, vector.y );
		}

		public static Vector3 GetRandomVector()
		{
			return new Vector3( Rand.Float( -1f, 1f ), Rand.Float( -1f, 1f ), Rand.Float( -1f, 1f ) ).Normal;
		}

		public static Vector2 GetRandomVector2()
		{
			return new Vector2( Rand.Float( -1f, 1f ), Rand.Float( -1f, 1f ) ).Normal;
		}

		public static float GetDegreesForDirection( Direction direction )
		{
			return ((int)direction - 1) * 90f;
		}

		public static void DrawCircle( Vector2 pos, float height, float radius, int segments, Color color, float startingAngle = 0f, float time = 0f )
		{
			float step = (2f * MathF.PI) / (float)segments;

			for ( int i = 1; i < segments + 1; i++ )
			{
				float toAngle = i * step + startingAngle;
				Vector2 toPoint = new Vector2(
					pos.x + radius * MathF.Cos( toAngle ),
					pos.y + radius * MathF.Sin( toAngle )
				);

				float fromAngle = (i - 1) * step + startingAngle;
				Vector2 fromPoint = new Vector2(
					pos.x + radius * MathF.Cos( fromAngle ),
					pos.y + radius * MathF.Sin( fromAngle )
				);

				DebugOverlay.Line(
					new Vector3( fromPoint.x, fromPoint.y, height ),
					new Vector3( toPoint.x, toPoint.y, height ),
					color,
					time
				);
			}
		}

		public static Trace TraceRayDirection( Vector3 from, Vector3 direction )
		{
			return Trace.Ray( from, from + direction.Normal * 100000f );
		}

		private class NodeInfo<T>
		{
			private const int MaxPoolSize = 8192;

			private static List<NodeInfo<T>> _sPool;

			internal static NodeInfo<T> Create( T node, NodeInfo<T> prev = null, float costAdd = 0f )
			{
				NodeInfo<T> nodeInfo;

				if ( _sPool == null || _sPool.Count == 0 )
				{
					nodeInfo = new NodeInfo<T>();
				}
				else
				{
					nodeInfo = _sPool[_sPool.Count - 1];
					_sPool.RemoveAt( _sPool.Count - 1 );
				}

				nodeInfo.Node = node;
				nodeInfo.Prev = prev;

				if ( prev == null )
				{
					nodeInfo.Depth = 0;
					nodeInfo.Cost = 0f;
				}
				else
				{
					nodeInfo.Depth = prev.Depth + 1;
					nodeInfo.Cost = prev.Cost + costAdd;
				}

				return nodeInfo;
			}

			internal static void Pool( NodeInfo<T> nodeInfo )
			{
				if ( _sPool == null ) _sPool = new List<NodeInfo<T>>( MaxPoolSize );
				if ( _sPool.Count >= MaxPoolSize ) return;

				_sPool.Add( nodeInfo );
			}

			private float _heuristic;

			public T Node { get; private set; }
			public NodeInfo<T> Prev { get; private set; }
			public int Depth { get; private set; }
			public float Cost { get; private set; }
			public float Total { get; private set; }

			public float Heuristic
			{
				get { return _heuristic; }
				set
				{
					_heuristic = value;
					Total = Cost + value;
				}
			}
		}

		/// <summary>
		/// Convenience method to produce a graph connection for use when calling AStar().
		/// </summary>
		/// <typeparam name="T">Graph node type.</typeparam>
		/// <param name="dest">Destination node of the connection.</param>
		/// <param name="cost">Cost of taking the connection.</param>
		public static AStarEdge<T> Edge<T>( T dest, float cost )
		{
			return new AStarEdge<T>( dest, cost );
		}

		private static class AStarWrapper<T>
			where T : IEquatable<T>
		{
			public static NodeInfo<T> FirstMatchOrDefault( List<NodeInfo<T>> list, T toCompare )
			{
				var count = list.Count;

				for ( var i = count - 1; i >= 0; --i )
				{
					var item = list[i];
					if ( item.Node.Equals( toCompare ) )
						return item;
				}

				return null;
			}

			private static List<NodeInfo<T>> _sOpen;
			private static List<NodeInfo<T>> _sClosed;

			public static bool AStar( T origin, T target, List<T> destList,
				Func<T, IEnumerable<AStarEdge<T>>> adjFunc, Func<T, T, float> heuristicFunc )
			{
				var open = _sOpen ?? (_sOpen = new List<NodeInfo<T>>());
				var clsd = _sClosed ?? (_sClosed = new List<NodeInfo<T>>());

				open.Clear();
				clsd.Clear();

				var first = NodeInfo<T>.Create( origin );
				first.Heuristic = heuristicFunc( origin, target );

				open.Add( first );

				try
				{
					while ( open.Count > 0 )
					{
						NodeInfo<T> cur = null;
						foreach ( var node in open )
						{
							if ( cur == null || node.Total < cur.Total ) cur = node;
						}

						if ( cur.Node.Equals( target ) )
						{
							for ( var i = cur.Depth; i >= 0; --i )
							{
								destList.Add( cur.Node );
								cur = cur.Prev;
							}
							destList.Reverse();
							return true;
						}

						open.Remove( cur );
						clsd.Add( cur );

						foreach ( var adj in adjFunc( cur.Node ) )
						{
							var node = NodeInfo<T>.Create( adj.Dest, cur, adj.Cost );
							var existing = FirstMatchOrDefault( clsd, adj.Dest );

							if ( existing != null )
							{
								if ( existing.Cost <= node.Cost ) continue;

								clsd.Remove( existing );
								node.Heuristic = existing.Heuristic;

								NodeInfo<T>.Pool( existing );
							}

							existing = FirstMatchOrDefault( open, adj.Dest );

							if ( existing != null )
							{
								if ( existing.Cost <= node.Cost ) continue;

								open.Remove( existing );
								node.Heuristic = existing.Heuristic;

								NodeInfo<T>.Pool( existing );
							}
							else
							{
								node.Heuristic = heuristicFunc( node.Node, target );
							}

							open.Add( node );
						}
					}
					return false;

				}
				finally
				{
					foreach ( var nodeInfo in open )
					{
						NodeInfo<T>.Pool( nodeInfo );
					}

					foreach ( var nodeInfo in clsd )
					{
						NodeInfo<T>.Pool( nodeInfo );
					}
				}
			}
		}

		/// <summary>
		/// An implementation of the AStar path finding algorithm.
		/// </summary>
		/// <typeparam name="T">Graph node type.</typeparam>
		/// <param name="origin">Node to start path finding from.</param>
		/// <param name="target">The goal node to reach.</param>
		/// <param name="adjFunc">Function returning the neighbouring connections for a node.</param>
		/// <param name="heuristicFunc">Function returning the estimated cost of travelling between two nodes.</param>
		/// <returns>A sequence of nodes representing a path if one is found, otherise an empty array.</returns>
		public static bool AStar<T>( T origin, T target, List<T> destPath,
			Func<T, IEnumerable<AStarEdge<T>>> adjFunc, Func<T, T, float> heuristicFunc )
			where T : IEquatable<T>
		{
			return AStarWrapper<T>.AStar( origin, target, destPath, adjFunc, heuristicFunc );
		}
	}

	public static class Extensions
	{
		public static float AngleRadians( this Vector2 vec )
		{
			return MathF.Atan2( vec.y, vec.x );
		}

		public static float AngleDegrees( this Vector2 vec )
		{
			return Utils.Rad2Deg( MathF.Atan2( vec.y, vec.x ) );
		}

		public static Vector2 PerpendicularClockwise( this Vector2 vector2 )
		{
			return new Vector2( -vector2.y, vector2.x );
		}

		public static Vector2 PerpendicularCounterClockwise( this Vector2 vector2 )
		{
			return new Vector2( vector2.y, -vector2.x );
		}

		public static void Shuffle<T>( this IList<T> list )
		{
			System.Random rng = new System.Random();
			int n = list.Count;
			while ( n > 1 )
			{
				n--;
				int k = rng.Next( n + 1 );
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		public static void DrawText( this Entity entity, string text, int offset, float duration = 0f)
		{
			// Client client = entity.GetClientOwner();
			// Color color = client == null ? Color.Black : AftermathGame.Instance.PlayerManager.GetPlayerData( client.NetworkIdent ).Color;
			if ( entity is Person person)
			{
				Player player = person.Player;
				Color color = player == null ? Color.Black : person.Player.TeamColor;
				float dist = 99999f;
				DebugOverlay.Text( entity.Position, offset, text, color, duration, dist );
			}
		}
	}
}
