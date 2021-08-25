using System;
using Sandbox;

namespace aftermath
{
	public readonly struct GridPosition : IEquatable<GridPosition>
	{
		public static readonly GridPosition Invalid = new GridPosition();
		public bool IsValid { get; }

		public readonly int X;
		public readonly int Y;

		public GridPosition( int x, int y )
		{
			this.X = x;
			this.Y = y;
			IsValid = true;
		}

		public GridPosition( float x, float y )
		{
			this.X = (int)x;
			this.Y = (int)y;
			IsValid = true;
		}

		public override string ToString()
		{
			return "(" + this.X.ToString() + ", " + this.Y.ToString() + ")";
		}

		public static GridPosition operator +( GridPosition a, GridPosition b ) { return new GridPosition( a.X + b.X, a.Y + b.Y ); }
		public static GridPosition operator -( GridPosition a, GridPosition b ) { return new GridPosition( a.X - b.X, a.Y - b.Y ); }
		public static bool operator ==( GridPosition a, GridPosition b ) { return a.Equals( b ); }
		public static bool operator !=( GridPosition a, GridPosition b ) { return !a.Equals( b ); }
		public bool Equals( GridPosition other ) { return X == other.X && Y == other.Y; }

		public override bool Equals( object obj )
		{
			if ( ReferenceEquals( null, obj ) ) return false;
			return obj is GridPosition && Equals( (GridPosition)obj );
		}

		public int ManhattanLength { get { return Math.Abs( X ) + Math.Abs( Y ); } }

		public override int GetHashCode()
		{
			unchecked
			{
				return (X * 397) ^ Y;
			}
		}
	}
}
