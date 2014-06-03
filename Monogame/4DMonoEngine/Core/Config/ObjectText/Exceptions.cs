using System;
using _4DMonoEngine.Core.Config.Parsing;
using PToken = _4DMonoEngine.Core.Config.Parsing.Token;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Thrown when there was an error parsing an ObjectText file.
	/// </summary>
	public class ObjectTextParseException : ParseException
	{
		#region Private Fields

		private readonly Token? _token;
		private readonly AbsolutePath _filepath;

		#endregion
		#region Constructors

		internal ObjectTextParseException() {}

		internal ObjectTextParseException(string message)
			: base(message) {}

		internal ObjectTextParseException(string message, Token token, AbsolutePath filepath)
			: base(message)
		{
			_token = token;
			_filepath = filepath;
		}

		internal ObjectTextParseException(string message, Exception innerException)
			: base(message, innerException) {}

		internal ObjectTextParseException(string message, Exception innerException, Token token, AbsolutePath filepath)
			: base(message, innerException)
		{
			_token = token;
			_filepath = filepath;
		}

		internal ObjectTextParseException(Token token, AbsolutePath filepath)
			: this( "Unexpected token " + StringTools.FormatString(token.Text, true) + " at position " + token.Position + " in file \"" + filepath + "\".", token, filepath) {}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the token that caused the error, or null if no token was involved.
		/// </summary>
		public Token? Token
		{
			get { return _token; }
		}

		/// <summary>
		/// Gets the path of the file that caused the error.
		/// </summary>
		public AbsolutePath FilePath
		{
			get { return _filepath; }
		}

		#endregion
	}

	/// <summary>
	/// Thrown when there was an error navigating an ObjectText file.
	/// </summary>
	public class ObjectTextNavigateException : ApplicationException
	{
		#region Constructors

		internal ObjectTextNavigateException() {}

		internal ObjectTextNavigateException(string message)
			: base(message) {}

		internal ObjectTextNavigateException(string message, Exception innerException)
			: base(message, innerException) {}

		#endregion
	}
}