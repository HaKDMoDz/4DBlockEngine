using System;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Extends the Group class to support inheritence.
	/// </summary>
	public abstract class GroupEx : Group
	{
		#region Private Fields

		private string _colon;
		private readonly InheritenceList _inheritenceList;
		private readonly string _beginBrace;
		private string _insignificant1;
		// <-- group body
		private readonly string _endBrace;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the list of references which defines the Groups that this Group inherits.
		/// </summary>
		public InheritenceList InheritenceList
		{
			get { return _inheritenceList; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Parses a new GroupEx.
		/// </summary>
		/// <param name="parent">The parent of this GroupEx.</param>
		/// <param name="colon">Optional initial colon specifying an inheritence list.</param>
		/// <param name="inheritenceList">Optional inheritence list.</param>
		/// <param name="beginBrace">Bracket marking the beginning of the body.</param>
		/// <param name="tok">Stream of input tokens.</param>
		internal GroupEx(Parent parent, Token colon, InheritenceList inheritenceList, Token beginBrace, Tokenizer tok)
			: base(parent)
		{
			// Validate tokens.
			if(colon.Text != ":" && colon.Text != "")
				throw new Exception("Internal error: Specified colon token is neither a colon nor an empty token.");
			if(beginBrace.Text != "{")
				throw new Exception("Internal error: The specified begin brace is not a brace.");

			// Set tokens.
			_colon = colon.Text;
			_inheritenceList = inheritenceList;
			_beginBrace = beginBrace.Text;
			inheritenceList.SetParent(this);

			// Subscribe to events.
			inheritenceList.LocalChildren.CountChanged += OnInheritenceListCountChanged;

			// Read initial insignificant, if any.
			Token t = tok.Read();

			// Parse.
			if(t.IsInsignificant())
			{
				_insignificant1 = t.Text;
				Token endBraceToken;
				Parse(tok.Read(), tok, BodyStopRule, out endBraceToken);
				_endBrace = endBraceToken.Text;
			}
			else
			{
				_insignificant1 = "";
				Token endBraceToken;
				Parse(t, tok, BodyStopRule, out endBraceToken);
				_endBrace = endBraceToken.Text;
			}
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this GroupEx.
		/// </summary>
		public override string GetDefiningText()
		{
			return _colon + _inheritenceList.GetDefiningText() + _beginBrace + _insignificant1 + base.GetDefiningText() + _endBrace;
		}

		/// <summary>
		/// Gets text that defines this GroupEx excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			return _colon + _inheritenceList.GetSignificantText() + _beginBrace + base.GetSignificantText() + _endBrace;
		}

		/// <summary>
		/// Gets the text that defines the body of this GroupEx.
		/// </summary>
		public override sealed string GetBodyText()
		{
			return _insignificant1 + base.GetBodyText();
		}

		/// <summary>
		/// Writes the text that defines this GroupEx to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(_colon);
			_inheritenceList.WriteTo(writer);
			writer.Write(_beginBrace);
			writer.Write(_insignificant1);
			base.WriteTo(writer);
			writer.Write(_endBrace);
		}

		/// <summary>
		/// Formats this GroupEx using the specified formatter.
		/// </summary>
		public override void Format(IFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");

			base.Format(formatter);
			_inheritenceList.Format(formatter);

			bool colon =_colon == ":";
			formatter.Format(this, ref colon, ref _insignificant1);
			if(colon == false && _inheritenceList.Count == 0)
				_colon = "";
			else
				_colon = ":";
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Appends the specified text to the initial insignificant body text.
		/// </summary>
		protected override sealed void AppendBodyInsignificant(string text)
		{
			_insignificant1 += text;
		}

		/// <summary>
		/// Gets the InheritenceList associated with this GroupEx.
		/// </summary>
		protected override sealed InheritenceList GetInheritenceList()
		{
			return _inheritenceList;
		}

		/// <summary>
		/// Return whether we should stop reading the list body at the specified token.
		/// </summary>
		private static bool BodyStopRule(Token token)
		{
			return token.Text == "}";
		}

		/// <summary>
		/// Called when the number of children in the inheritence list changes.
		/// </summary>
		private void OnInheritenceListCountChanged(object sender, EventArgs e)
		{
			if(_colon == "" && _inheritenceList.Count > 0)
				_colon = ":";
		}

		#endregion
	}
}