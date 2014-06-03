using System;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A Reference node contained by an InheritenceList.
	/// </summary>
	public class InheritenceReference : Reference
	{
		#region Private Fields

		private readonly InheritenceList _parent;

		// <-- reference body
		private string _insignificant1;
		private string _terminator;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the InheritenceList that contains this InheritenceReference.
		/// </summary>
		public new InheritenceList Parent
		{
			get { return _parent; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Parses a new InheritenceReference.
		/// </summary>
		/// <param name="parent">The parent List of this InheritenceReference.</param>
		/// <param name="init">Initial value token of this field.</param>
		/// <param name="tok">Stream of input tokens.</param>
		/// <param name="parentStop">Parent's rule defining when to stop reading.</param>
		/// <param name="out1">Insignificant text before parent list's stop token, or null if no stop token was read.</param>
		/// <param name="out2">Parent list's end-bracket, or null if none was read.</param>
		internal InheritenceReference(InheritenceList parent, Token init, Tokenizer tok, StopRule parentStop, out Token? out1,
		                              out Token? out2)
			: base(parent)
		{
			_parent = parent;

			// Parse.
			Token t1, t2;
			Parse(init, tok, StopRule, parentStop, out t1, out t2);

			// Did we read the parent's stop token?
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
		/// Gets the text that defines this InheritenceReference.
		/// </summary>
		public override string GetDefiningText()
		{
			return base.GetDefiningText() + _insignificant1 + _terminator;
		}

		/// <summary>
		/// Gets text that defines this InheritenceReference excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			return base.GetSignificantText() + _terminator;
		}

		/// <summary>
		/// Writes the text that defines this InheritenceReference to the specified TextWriter.
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
		/// Formats this InheritenceReference using the specified formatter.
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

		/// <summary>
		/// Finds the node directly referenced by this InheritenceReference.
		/// </summary>
		/// <returns>The found node regardless of whether it itself is a reference.</returns>
		public override INode FindNextTarget()
		{
			return Parent.Parent.Parent.FindAtPath("&" + Target);
		}

		/// <summary>
		/// Attempts to find the node directly referenced by this InheritenceReference.
		/// </summary>
		/// <param name="node">The found node or null if it was not found.</param>
		/// <returns>Whether the node was found.</returns>
		public override bool TryFindNextTarget(out INode node)
		{
			return Parent.Parent.Parent.TryFindAtPath("&" + Target, out node);
		}

		/// <summary>
		/// Returns whether there exists a node at the path specified by this InheritenceReference.
		/// </summary>
		public override bool NextTargetExists()
		{
			return Parent.Parent.Parent.ExistsAtPath("&" + Target);
		}

		/// <summary>
		/// Finds the final node referenced by this InheritenceReference.
		/// </summary>
		/// <returns>The found node or if itself is a Reference then the node it finally references.</returns>
		public override INode FindFinalTarget()
		{
			return Parent.Parent.Parent.FindAtPath(Target);
		}

		/// <summary>
		/// Attempts to find the final node referenced by this InheritenceReference.
		/// </summary>
		/// <param name="node">The found node or null if it was not found.</param>
		/// <returns>Whether the node was found.</returns>
		public override bool TryFindFinalTarget(out INode node)
		{
			return Parent.Parent.Parent.TryFindAtPath(Target, out node);
		}

		/// <summary>
		/// Returns whether there exists a node at the path specified by this InheritenceReference or by any other Reference which this InheritenceReference directly or indirectly references.
		/// </summary>
		public override bool FinalTargetExists()
		{
			return Parent.Parent.Parent.ExistsAtPath(Target);
		}

		#endregion
		#region Private Methods

		/// <summary>
		/// Gets whether this InheritenceReference has a terminating comma or semicolon.
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