using System;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;
using PTokenizerFSM  = _4DMonoEngine.Core.Config.Parsing.TokenizerFSM;
using PTokenizer  = _4DMonoEngine.Core.Config.Parsing.Tokenizer;
using PToken = _4DMonoEngine.Core.Config.Parsing.Token;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Parses tokens from a ObjectText file.
	/// </summary>
	public class Tokenizer
	{
		#region Private Fields

		private static readonly PTokenizerFSM _fsm;

		private readonly PTokenizer _tok;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the reader from which this tokenizer reads tokens.
		/// </summary>
		public BinaryReader Stream
		{
			get { return _tok.Reader; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Static constructor.
		/// </summary>
		static Tokenizer()
		{
			Predicate<int> isWhitespace = input => input == ' ' || input == '\n' || input == '\r' || input == '\t';
			Predicate<int> isIdentifier = input => (input >= 'a' && input <= 'z') || (input >= 'A' && input <= 'Z') || (input >= '0' && input <= '9') || input == '_' || input == '#';
			Predicate<int> isSlash = input => input == '/';
			Predicate<int> isBackSlash = input => input == '\\';
			Predicate<int> isStar = input => input == '*';
			Predicate<int> isCR = input => input == '\n';
			Predicate<int> isQuote = input => input == '"';
			Predicate<int> isAt = input => input == '@';

			_fsm = new PTokenizerFSM(
				new State(StateAction.Add, 2, Transition.Return,
					new Transition(isIdentifier, 1),
					new Transition(isWhitespace, 3),
					new Transition(isSlash, 6),
					new Transition(isQuote, 10),
					new Transition(isAt, 13)),
				new State(StateAction.Add, Transition.Return, Transition.Return,
					new Transition(isIdentifier, 1)),
				new State(StateAction.Add, Transition.Return, Transition.Return),
				new State(StateAction.Add, Transition.Return, Transition.Return,
					new Transition(isWhitespace, 3),
					new Transition(isSlash, 4)),
				new State(StateAction.Add, 5, 5,
					new Transition(isSlash, 7),
					new Transition(isStar, 8)),
				new State(StateAction.Back, Transition.Return, Transition.Return),
				new State(StateAction.Add, Transition.Return, Transition.Return,
					new Transition(isSlash, 7),
					new Transition(isStar, 8)),
				new State(StateAction.Add, 7, Transition.Return,
					new Transition(isCR, 3)),
				new State(StateAction.Add, 8, Transition.Return,
					new Transition(isStar, 9)),
				new State(StateAction.Add, 8, Transition.Return,
					new Transition(isSlash, 3)),
				new State(StateAction.Add, 10, Transition.Error,
					new Transition(isQuote, 11),
					new Transition(isBackSlash, 12),
					new Transition(isCR, Transition.Error)),
				new State(StateAction.Add, Transition.Return, Transition.Return),
				new State(StateAction.Add, 10, Transition.Error,
					new Transition(isCR, Transition.Error)),
				new State(StateAction.Add, Transition.Return, Transition.Return,
					new Transition(isQuote, 14)),
				new State(StateAction.Add, 14, Transition.Error,
					new Transition(isQuote, 13))
			);
		}

		/// <summary>
		/// Creates a new Tokenizer that reads tokens from the specified StreamReader.
		/// </summary>
		public Tokenizer(BinaryReader reader)
		{
			_tok = new PTokenizer(reader, _fsm);
		}

		/// <summary>
		/// Creates a new tokenizer that reads tokens from the specified StreamReader.
		/// </summary>
		public Tokenizer(BinaryReader reader, LineCharPosition pos)
		{
			_tok = new PTokenizer(reader, pos, _fsm);
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Reads a token from the StreamReader.
		/// </summary>
		public Token Read()
		{
			return _tok.Read();
		}

		#endregion
	}
}