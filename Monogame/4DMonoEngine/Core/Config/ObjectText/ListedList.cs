using System;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A List node contained by a List node.
	/// </summary>
	public class ListedList : ListEx, IListedNodeEx
	{
		#region Private Fields

		private List _parent;

		// <-- ListEx stuff
		private string _insignificant1;
		private string _terminator;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the List that contains this ListedList.
		/// </summary>
		public new List Parent
		{
			get { return _parent; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Parses a new ListedList.
		/// </summary>
		/// <param name="parent">The parent List of this ListedList.</param>
		/// <param name="colon">Optional initial colon specifying an inheritence list.</param>
		/// <param name="inheritenceList">Optional inheritence list.</param>
		/// <param name="beginBracket">Bracket marking the beginning of the body.</param>
		/// <param name="tok">Stream of input tokens.</param>
		/// <param name="out1">Insignificant text before next significant token, or null if none was read.</param>
		/// <param name="out2">Next significant token, or null if none was read.</param>
		internal ListedList(List parent, Token colon, InheritenceList inheritenceList, Token beginBracket, Tokenizer tok,
		                    out Token? out1, out Token? out2)
			: base(parent, colon, inheritenceList, beginBracket, tok)
		{
			_parent = parent;

			// Read optional insignificant and comma.
			Token t = tok.Read();
			Token t2;
			if(t.IsInsignificant())
			{
				t2 = t;
				t = tok.Read();
			}
			else
			{
				t2 = Token.Empty;
			}

			// Did we read a comma?
			if(t.Text == "," || t.Text == ";")
			{
				// Yes, and signify that by passing up null to the parent list.
				out1 = null;
				out2 = null;

				// Set tokens accordingly.
				_insignificant1 = t2.Text;
				_terminator = t.Text;
			}
			else
			{
				// No, and signify that by passing up the insignificant and significant tokens to the parent list.
				out1 = t2;
				out2 = t;

				// Set tokens accordingly.
				_insignificant1 = "";
				_terminator = "";
			}
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this ListedList.
		/// </summary>
		public override string GetDefiningText()
		{
			return base.GetDefiningText() + _insignificant1 + _terminator;
		}

		/// <summary>
		/// Gets text that defines this ListedList excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			return base.GetSignificantText() + _terminator;
		}

		/// <summary>
		/// Writes the text that defines this ListedList to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			base.WriteTo(writer);
			writer.Write(_insignificant1);
			writer.Write(_terminator);
		}

		/// <summary>
		/// Formats this ListedList using the specified formatter.
		/// </summary>
		public override void Format(IFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");

			base.Format(formatter);

			formatter.Format(this, ref _insignificant1, ref _terminator);

			if(_insignificant1 == null || !string.IsNullOrWhiteSpace(_insignificant1))
				throw new InvalidOperationException("insignificant1 must only contain whitespace characters.");
			if(_terminator == null || (_terminator != ";" && _terminator != "," && _terminator != ""))
				throw new InvalidOperationException("terminator must be a semicolon, a comma, or an empty string");
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Sets the parent of this ListedList.
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

		#endregion
	}
}