using System;

namespace _4DMonoEngine.Core.Config.Parsing
{
	/// <summary>
	/// Stores a position within a file.
	/// </summary>
	public struct LineCharPosition : IEquatable<LineCharPosition>, IComparable<LineCharPosition>
	{
		#region Public Static Fields

		public static readonly LineCharPosition Initial = new LineCharPosition(1, 1);

		#endregion
		#region Public Fields

		public readonly int Line;
		public readonly int Char;

		#endregion
		#region Constructors

		/// <summary>
		/// Initializes a new LineCharPosition.
		/// </summary>
		public LineCharPosition(int line, int cha)
		{
			Line = line;
			Char = cha;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Returns whether this LineCharPosition is equal to the specified object.
		/// </summary>
		public override bool Equals(object obj)
		{
			if(obj is LineCharPosition)
				return Equals((LineCharPosition)obj);
			else
				return false;
		}

		/// <summary>
		/// Returns whether this LineCharPosition is equal to the specified LineCharPosition.
		/// </summary>
		public bool Equals(LineCharPosition lcp)
		{
			return Line == lcp.Line && Char == lcp.Char;
		}

		/// <summary>
		/// Returns the hash code for this LineCharPosition.
		/// </summary>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + Line.GetHashCode();
				hash = hash * 23 + Char.GetHashCode();
				return hash;
			}
		}

		/// <summary>
		/// Compares this LineCharPosition to the specified LineCharPosition and returns the result.
		/// </summary>
		public int CompareTo(LineCharPosition lcp)
		{
			int res = Line.CompareTo(lcp.Line);
			if(res != 0)
				return res;
			return Char.CompareTo(lcp.Char);
		}

		/// <summary>
		/// Returns the string representation of this position.
		/// </summary>
		public override string ToString()
		{
			return "{Line=" + Line + ",Char=" + Char + "}";
		}

		/// <summary>
		/// Returns a new position advanced based on the specififed input.
		/// </summary>
		public LineCharPosition Advance(char input)
		{
			if(input == '\n')
				return new LineCharPosition(Line + 1, 1);
			else
				return new LineCharPosition(Line, Char + 1);
		}

		#endregion
		#region Operators

		/// <summary>
		/// Returns whether the specified LineCharPosition objects are equal.
		/// </summary>
		public static bool operator ==(LineCharPosition lcp1, LineCharPosition lcp2)
		{
			return lcp1.Equals(lcp2);
		}

		/// <summary>
		/// Returns whether the specified LineCharPosition objects are not equal.
		/// </summary>
		public static bool operator !=(LineCharPosition lcp1, LineCharPosition lcp2)
		{
			return !lcp1.Equals(lcp2);
		}

		#endregion
	}
}