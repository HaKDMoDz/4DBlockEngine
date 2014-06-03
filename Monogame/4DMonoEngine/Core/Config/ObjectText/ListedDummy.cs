using System;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A Dummy node contained by a List node.
	/// </summary>
	public class ListedDummy : Dummy, IListedNodeEx
	{
		#region Private Fields

		private List _parent;

		private readonly string _questionMark;
		private string _insignificant1;
		private string _terminator;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the List that contains this ListedDummy.
		/// </summary>
		public new List Parent
		{
			get { return _parent; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Parses a new ListedDummy.
		/// </summary>
		/// <param name="parent">The parent List of this ListedDummy.</param>
		/// <param name="questionMark">Question mark token.</param>
		/// <param name="tok">Stream of input tokens.</param>
		/// <param name="parentStop">Parent's rule defining when to stop reading.</param>
		/// <param name="out1">Insignificant text before parent list's end-bracket, or null if no end-bracket was read.</param>
		/// <param name="out2">Parent list's end-bracket, or null if none was read.</param>
		internal ListedDummy(List parent, Token questionMark, Tokenizer tok, StopRule parentStop, out Token? out1, out Token? out2)
			: base(parent)
		{
			_parent = parent;

			// Validate tokens.
			if(questionMark.Text != "?")
				throw new Exception("Internal error: The specified questionMark token is not a question mark.");

			// Set tokens.
			_questionMark = questionMark.Text;

			// Parse.
			Token t2, t3;
			Token t = tok.Read();
			if(t.IsInsignificant())
			{
				t2 = t;
				t3 = tok.Read();

				// Verify that we read a comma or an end-bracket.
				if(!StopRule(t3) && !(parentStop(t3)))
					throw new ObjectTextParseException(t, FileRoot.FilePath);
			}
			else
			{
				t2 = Token.Empty;
				t3 = t;
			}

			// Did we read the parent's stop token?
			if(parentStop(t3))
			{
				// Yes, so pass up the whitespace and bracket to the parent list.
				out1 = t2;
				out2 = t3;

				// Set tokens accordingly.
				_insignificant1 = "";
				_terminator = "";
			}
			else
			{
				// No, and signify that by passing up null to the parent list.
				out1 = null;
				out2 = null;

				// Set tokens accordingly.
				_insignificant1 = t2.Text;
				_terminator = t3.Text;
			}
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text which defines this ListedDummy.
		/// </summary>
		public override string GetDefiningText()
		{
			return _questionMark + _insignificant1 + _terminator;
		}

		/// <summary>
		/// Gets text that defines this ListedDummy excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			return _questionMark + _terminator;
		}

		/// <summary>
		/// Writes the text that defines this ListedDummy to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(_questionMark);
			writer.Write(_insignificant1);
			writer.Write(_terminator);
		}

		/// <summary>
		/// Formats this ListedDummy using the specified formatter.
		/// </summary>
		public override void Format(IFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");

			formatter.Format(this, ref _insignificant1, ref _terminator);

			if(_insignificant1 == null || !string.IsNullOrWhiteSpace(_insignificant1))
				throw new InvalidOperationException("insignificant1 must only contain whitespace characters.");
			if(_terminator == null || (_terminator != ";" && _terminator != "," && _terminator != ""))
				throw new InvalidOperationException("terminator must be a semicolon, a comma, or an empty string");
			if(!HasTerminator && !IsLastLocalNodeInParent)
				throw new InvalidOperationException("Terminator not specified but was required.");
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Sets the parent of this ListedDummy.
		/// </summary>
		internal override void SetParent(Parent parent)
		{
			base.SetParent(parent);
			_parent = (List)parent;
		}

		/// <summary>
		/// Gets or sets whether this node has a terminating comma or semicolon.
		/// </summary>
		bool IListedNodeEx.HasTerminator
		{
			get { return HasTerminator; }
			set { HasTerminator = value; }
		}

		/// <summary>
		/// Gets or sets whether this node has a terminating comma or semicolon.
		/// </summary>
		internal bool HasTerminator
		{
			get { return _terminator != ""; }
			set
			{
				if(value != (_terminator != ""))
					_terminator = value ? "," : "";
			}
		}

		/// <summary>
		/// Returns whether we should stop reading at the specified token.
		/// </summary>
		private static bool StopRule(Token token)
		{
			return token.Text == "," || token.Text == ";";
		}

		#endregion
	}
}