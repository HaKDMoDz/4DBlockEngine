using System;

namespace _4DMonoEngine.Core.Config.Parsing
{
	/// <summary>
	/// Stores the contents of a single string token read from a character stream.
	/// </summary>
	public struct Token : IEquatable<Token>
	{
		#region Public Constants

		public static readonly Token Empty = new Token(string.Empty);

		#endregion
		#region Private Fields

		private readonly string _text;
		private readonly LineCharPosition _pos;

		#endregion
		#region Properties

		/// <summary>
		/// Gets whether this Token represents the end of the file.
		/// </summary>
		public bool IsEndOfFile
		{
			get { return _text.Length == 0; }
		}

		/// <summary>
		/// Gets this Token's text.
		/// </summary>
		public string Text
		{
			get { return _text ?? string.Empty; }
		}

		/// <summary>
		/// Gets the line and character position of this Token in the file from which it was parsed.
		/// </summary>
		public LineCharPosition Position
		{
			get { return _pos; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Creates a new Token initialized to the specified text.
		/// </summary>
		public Token(string text)
		{
			_text = text;
			_pos = new LineCharPosition();
		}

		/// <summary>
		/// Creates a new Token initialized to the specified text and position.
		/// </summary>
		public Token(string text, LineCharPosition pos)
		{
			_text = text;
			_pos = pos;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Returns whether this Token is equal to the specified object.
		/// </summary>
		public override bool Equals(object obj)
		{
			if(obj is Token)
				return Equals((Token)obj);
			else
				return false;
		}

		/// <summary>
		/// Returns whether this Token is equal to the specified Token.
		/// </summary>
		public bool Equals(Token token)
		{
			return Text == token.Text && Position == token.Position;
		}

		/// <summary>
		/// Returns the hash code for this Token.
		/// </summary>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + Text.GetHashCode();
				hash = hash * 23 + Position.GetHashCode();
				return hash;
			}
		}

		/// <summary>
		/// Returns the string representation of this Token.
		/// </summary>
		public override string ToString()
		{
			return "{" + StringTools.FormatString(Text, true) + "," + Position + "}";
		}

		#endregion
		#region Operators

		/// <summary>
		/// Returns whether the specified Token objects are equal.
		/// </summary>
		public static bool operator ==(Token token1, Token token2)
		{
			return token1.Equals(token2);
		}

		/// <summary>
		/// Returns whether the specified Token objects are not equal.
		/// </summary>
		public static bool operator !=(Token token1, Token token2)
		{
			return !token1.Equals(token2);
		}

		#endregion
	}
}