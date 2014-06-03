using System;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A Field node contained by a List node.
	/// </summary>
	public class ListedField : Field, IListedNodeEx
	{
		#region Private Fields

		private List _parent;

		// <-- field body
		private string _insignificant1;
		private string _terminator;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the List that contains this ListedField.
		/// </summary>
		public new List Parent
		{
			get { return _parent; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Parses a new ListedField.
		/// </summary>
		/// <param name="parent">The parent List of this ListedField.</param>
		/// <param name="init">Initial value token of this field.</param>
		/// <param name="tok">Stream of input tokens.</param>
		/// <param name="parentStop">Parent's rule defining when to stop reading.</param>
		/// <param name="out1">Insignificant text before parent list's stop token, or null if no stop token was read.</param>
		/// <param name="out2">Parent list's end-bracket, or null if none was read.</param>
		internal ListedField(List parent, Token init, Tokenizer tok, StopRule parentStop, out Token? out1, out Token? out2)
			: base(parent)
		{
			_parent = parent;

			// Parse.
			Token t1, t2;
			Parse(init, tok, StopRule, parentStop, out t1, out t2);

			// Did we read the list's end-bracket?
			if(parentStop(t2))
			{
				// Yes, so pass up the whitespace and bracket to the parent list.
				out1 = t1;
				out2 = t2;

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
				_insignificant1 = t1.Text;
				_terminator = t2.Text;
			}
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this ListedField.
		/// </summary>
		public override string GetDefiningText()
		{
			return base.GetDefiningText() + _insignificant1 + _terminator;
		}

		/// <summary>
		/// Gets text that defines this ListedField excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			return base.GetSignificantText() + _terminator;
		}

		/// <summary>
		/// Writes the text that defines this ListedField to the specified TextWriter.
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
		/// Formats this ListedField using the specified formatter.
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
		/// Sets the parent of this ListedField.
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