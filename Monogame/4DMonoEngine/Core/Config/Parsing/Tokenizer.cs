using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace _4DMonoEngine.Core.Config.Parsing
{
	/// <summary>
	/// Uses a TokenizerFSM to parse input streams into sequences of tokens.
	/// </summary>
	public class Tokenizer
	{
		#region Private Fields

		private readonly BinaryReader _reader;
		private readonly TokenizerFSM _fsm;
		private readonly List<ProcessedChar> _chars = new List<ProcessedChar>();
		private readonly StringBuilder _ret = new StringBuilder();
		private LineCharPosition _pos;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the stream from which this Tokenizer reads tokens.
		/// </summary>
		public BinaryReader Reader
		{
			get { return _reader; }
		}

		/// <summary>
		/// Gets the TokenizerFSM used to read tokens from the stream.
		/// </summary>
		public TokenizerFSM TokenizerFSM
		{
			get { return _fsm; }
		}

		/// <summary>
		/// Gets or sets the current line and character position to be recorded with tokens.
		/// </summary>
		public LineCharPosition Position
		{
			get { return _pos; }
			set { _pos = value; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Creates a new Tokenizer which parses an input stream using the specified TokenizerFSM.
		/// </summary>
		public Tokenizer(BinaryReader reader, TokenizerFSM fsm)
			: this(reader, LineCharPosition.Initial, fsm)
		{
			// Do nothing extra.
		}

		/// <summary>
		/// Creates a new Tokenizer which parses an input stream using the specified FSM.
		/// </summary>
		public Tokenizer(BinaryReader reader, LineCharPosition initialPos, TokenizerFSM fsm)
		{
			if(reader == null)
				throw new ArgumentNullException("reader");
			if(fsm == null)
				throw new ArgumentNullException("fsm");
			if(!reader.BaseStream.CanRead)
				throw new ArgumentException("The specified stream does not support reading.");
			if(!reader.BaseStream.CanSeek)
				throw new ArgumentException("The specified stream does not support seeking.");

			_reader = reader;
			_pos = initialPos;
			_fsm = fsm;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Reads and returns a single token from the stream.
		/// </summary>
		public Token Read()
		{
			LineCharPosition oldPos = _pos;
			State cur = _fsm.States[0];
			_ret.Clear();
			_chars.Clear();

			// Process stream into list of characters.
			bool done = false;
			do
			{
				int input = _reader.PeekChar();
				int next = cur.GetNextState(input);
				switch(next)
				{
					case Transition.Return:
						done = true;
						break;
					case Transition.Error:
						for(int i = 0; i < _ret.Length; i++)
							_pos.Advance(_ret[i]);
						_pos.Advance((char)input);
						throw new TokenizeException(input, _pos);
					default:
						cur = _fsm.States[next];
						switch(cur.Action)
						{
							case StateAction.Add:
								_chars.Add(new ProcessedChar((char)input, false));
								_reader.Read();
								break;
							case StateAction.Skip:
								_chars.Add(new ProcessedChar((char)input, true));
								_reader.Read();
								break;
							case StateAction.Back:
								_chars.RemoveAt(_chars.Count - 1);
								_reader.BaseStream.Position -= 1;
								break;
							default:
								throw new NotSupportedException("StateAction '" + cur.Action + "' is not supported.");
						}
						break;
				}
			}
			while(!done);

			// Copy processed characters to string builder.
			for(int i = 0; i < _chars.Count; i++)
			{
				ProcessedChar pc = _chars[i];
				if(!pc.Skipped)
					_ret.Append(pc.Value);
				_pos = _pos.Advance(pc.Value);
			}
			return new Token(_ret.ToString(), oldPos);
		}

		#endregion
		#region Private Types

		/// <summary>
		/// Stores a processed character.
		/// </summary>
		private struct ProcessedChar
		{
			public readonly char Value;
			public readonly bool Skipped;

			public ProcessedChar(char value, bool skipped)
			{
				Value = value;
				Skipped = skipped;
			}
		}

		#endregion
	}
}