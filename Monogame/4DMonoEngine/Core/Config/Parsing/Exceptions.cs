using System;

namespace _4DMonoEngine.Core.Config.Parsing
{
	/// <summary>
	/// Thrown when there was an error parsing an input stream.
	/// </summary>
	public class ParseException : ApplicationException
	{
		#region Constructors

		public ParseException() {}
		public ParseException(string message) : base(message) {}
		public ParseException(string message, Exception innerException) : base(message, innerException) {}

		#endregion
	}

	/// <summary>
	/// Thrown when there was an error reading a token from a stream.
	/// </summary>
	public class TokenizeException : ParseException
	{
		#region Private Fields

		private readonly int _input;
		private readonly LineCharPosition _pos;

		#endregion
		#region Constructors

		/// <summary>
		/// Creates a new TokenizeException.
		/// </summary>
		public TokenizeException(int input, LineCharPosition pos)
			: base("An unexpected character (" + input + ") was found at position " + pos + " while parsing the stream.")
		{
			_input = input;
			_pos = pos;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the input character that caused the error.
		/// </summary>
		public int Input
		{
			get { return _input; }
		}

		/// <summary>
		/// Gets position within the stream of the input character.
		/// </summary>
		public LineCharPosition Position
		{
			get { return _pos; }
		}

		#endregion
	}

	/// <summary>
	/// Thrown when there was an error parsing a command-line argument.
	/// </summary>
	public class CmdLineArgumentParseException : ParseException
	{
		#region Constructors

		public CmdLineArgumentParseException() {}
		public CmdLineArgumentParseException(string message) : base(message) {}
		public CmdLineArgumentParseException(string message, Exception innerException) : base(message, innerException) {}

		#endregion
	}
}