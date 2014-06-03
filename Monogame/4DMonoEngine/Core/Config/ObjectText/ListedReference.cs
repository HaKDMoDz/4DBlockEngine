using System;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A Reference node contained by a List node.
	/// </summary>
	public class ListedReference : Reference, IListedNodeEx
	{
		#region Private Fields

		private List _parent;

		private readonly string _ampersand;
		private string _insignificant1;
		// <-- reference body
		private string _insignificant2;
		private string _terminator;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the List that contains this ListedReference.
		/// </summary>
		public new List Parent
		{
			get { return _parent; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Parses a new ListedReference.
		/// </summary>
		/// <param name="parent">The parent List of this ListedReference.</param>
		/// <param name="ampersand">Ampersand token.</param>
		/// <param name="tok">Stream of input tokens.</param>
		/// <param name="parentStop">Parent's rule defining when to stop reading.</param>
		/// <param name="out1">Whitespace before parent list's end-bracket, or null if no end-bracket was read.</param>
		/// <param name="out2">Parent list's end-bracket, or null if none was read.</param>
		internal ListedReference(List parent, Token ampersand, Tokenizer tok, StopRule parentStop, out Token? out1, out Token? out2)
			: base(parent)
		{
			_parent = parent;

			// Validate tokens.
			if(ampersand.Text != "&")
				throw new Exception("Internal Error: The specified ampersand token is not an ampersand.");

			// Set tokens.
			_ampersand = ampersand.Text;

			// Read initial insignificant, if any.
			Token t = tok.Read();

			// Parse.
			Token t2, t3;
			if(t.IsInsignificant())
			{
				_insignificant1 = t.Text;
				Parse(tok.Read(), tok, StopRule, parentStop, out t2, out t3);
			}
			else
			{
				_insignificant1 = "";
				Parse(t, tok, StopRule, parentStop, out t2, out t3);
			}

			// Did we read the list's end-bracket?
			if(parentStop(t3))
			{
				// Yes, so pass up the whitespace and bracket to the parent list.
				out1 = t2;
				out2 = t3;

				// Set tokens accordingly.
				_insignificant2 = "";
				_terminator = "";
			}
			else
			{
				// No, and signify that by passing up null to the parent list.
				out1 = null;
				out2 = null;

				// Set tokens accordingly.
				_insignificant2 = t2.Text;
				_terminator = t3.Text;
			}
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this ListedReference.
		/// </summary>
		public override string GetDefiningText()
		{
			return _ampersand + _insignificant1 + base.GetDefiningText() + _insignificant2 + _terminator;
		}

		/// <summary>
		/// Gets text that defines this ListedReference excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			return _ampersand + base.GetSignificantText() + _terminator;
		}

		/// <summary>
		/// Writes the text that defines this ListedReference to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(_ampersand);
			writer.Write(_insignificant1);
			base.WriteTo(writer);
			writer.Write(_insignificant2);
			writer.Write(_terminator);
		}

		/// <summary>
		/// Formats this ListedReference using the specified formatter.
		/// </summary>
		public override void Format(IFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");

			formatter.Format(this, ref _insignificant1, ref _insignificant2, ref _terminator);

			if(_insignificant1 == null || !string.IsNullOrWhiteSpace(_insignificant1))
				throw new InvalidOperationException("insignificant1 must only contain whitespace characters.");
			if(_insignificant2 == null || !string.IsNullOrWhiteSpace(_insignificant2))
				throw new InvalidOperationException("insignificant2 must only contain whitespace characters.");
			if(_terminator == null || (_terminator != ";" && _terminator != "," && _terminator != ""))
				throw new InvalidOperationException("terminator must be a semicolon, a comma, or an empty string");
			if(!HasTerminator && !IsLastLocalNodeInParent)
				throw new InvalidOperationException("Terminator not specified but was required.");
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Sets the parent of this ListedReference.
		/// </summary>
		internal override void SetParent(Parent parent)
		{
			base.SetParent(parent);
			_parent = (List)parent;
		}

		/// <summary>
		/// Gets or sets whether this ListedReference has a terminating comma.
		/// </summary>
		bool IListedNodeEx.HasTerminator
		{
			get { return HasTerminator; }
			set { HasTerminator = value; }
		}

		/// <summary>
		/// Gets or sets whether this ListedReference has a terminating comma.
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