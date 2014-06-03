using System;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A Field node contained within a Group node.
	/// </summary>
	public class GroupedField : Field, IGroupedNodeEx
	{
		#region Events

		/// <summary>
		/// Invoked when the name of this GroupedField has changed.
		/// </summary>
		public event EventHandler<EventArgs> NameChanged;

		#endregion
		#region Private Fields

		private Group _parent;
		
		private string _name;
		private string _insignificant1;
		private readonly string _equals;
		private string _insignificant2;
		// <-- field body
		private string _insignificant3;
		private string _terminator;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the Group containing this GroupedField.
		/// </summary>
		public new Group Parent
		{
			get { return _parent; }
		}

		/// <summary>
		/// Gets or sets the name of this GroupedField.
		/// </summary>
		public new string Name
		{
			get { return _name; }
			set
			{
				if(value != _name)
				{
					if(!value.IsIdentifier())
						throw new ArgumentException("The specified name \"" + value + "\" is not a valid identifier.");
					UnregisterHashTag();
					_parent.RenameChild(_name, value);
					_name = value;
					RegisterHashTag();

					InvokeNameChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets whether the name of this node has been hash-tagged and can thus be accessed with "#name" from anywhere.
		/// </summary>
		public new bool HasHashTag
		{
			get { return p_hasHashTag; }
			set
			{
				if(value != p_hasHashTag)
				{
					UnregisterHashTag();
					p_hasHashTag = value;
					RegisterHashTag();
				}
			}
		}
		private bool p_hasHashTag;

		#endregion
		#region Constructors

		/// <summary>
		/// Parses a new GroupedField.
		/// </summary>
		/// <param name="parent">The parent Group of this GroupedField.</param>
		/// <param name="name">Name token of this field.</param>
		/// <param name="insignificant1">Insignificant text between name token and equals sign.</param>
		/// <param name="equals">Equals sign token.</param>
		/// <param name="insignificant2">Insignificant text between equals sign and value.</param>
		/// <param name="init">Initial value token of this field.</param>
		/// <param name="tok">Stream of input tokens.</param>
		/// <param name="parentStop">Parent's rule defining when to stop reading.</param>
		/// <param name="out1">Whitespace before parent group's end-brace, or null if no end-brace was read.</param>
		/// <param name="out2">Parent group's end-brace, or null if none was read.</param>
		internal GroupedField(Group parent, Token name, Token insignificant1, Token equals, Token insignificant2, Token init,
		                      Tokenizer tok, StopRule parentStop, out Token? out1, out Token? out2)
			: base(parent)
		{
			_parent = parent;

			// Validate tokens.
			if(!name.IsIdentifier())
				throw new Exception("Internal error: The specified name token is not an identifier.");
			if(!insignificant1.IsInsignificant())
				throw new Exception("Internal error: The specified insignificant1 token is not insignificant.");
			if(equals.Text != "=")
				throw new Exception("Internal error: The specified equals token is not an equals sign.");
			if(!insignificant2.IsInsignificant())
				throw new Exception("Internal error: The specified insignificant2 token is not insignificant.");

			// Set tokens.
			_name = name.Text.Unhashed();
			_insignificant1 = insignificant1.Text;
			_equals = equals.Text;
			_insignificant2 = insignificant2.Text;
			HasHashTag = name.IsHashTagged();

			// Parse.
			Token t1, t2;
			Parse(init, tok, StopRule, parentStop, out t1, out t2);

			// Did we read the group's end-brace?
			if(parentStop(t2))
			{
				// Yes, so pass up the whitespace and brace to the parent group.
				out1 = t1;
				out2 = t2;

				// Set tokens accordingly.
				_insignificant3 = "";
				_terminator = "";
			}
			else
			{
				// No, and signify that by passing up null to the parent list.
				out1 = null;
				out2 = null;

				// Set tokens accordingly.
				_insignificant3 = t1.Text;
				_terminator = t2.Text;
			}
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this GroupedField.
		/// </summary>
		public override string GetDefiningText()
		{
			return (HasHashTag ? "#" : "") + _name + _insignificant1 + _equals + _insignificant2 + base.GetDefiningText() + _insignificant3 + _terminator;
		}

		/// <summary>
		/// Gets text that defines this GroupedField excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			return (HasHashTag ? "#" : "") + _name + _equals + base.GetSignificantText() + _terminator;
		}

		/// <summary>
		/// Writes the text that defines this GroupedField to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			if(HasHashTag)
				writer.Write('#');
			writer.Write(_name);
			writer.Write(_insignificant1);
			writer.Write(_equals);
			writer.Write(_insignificant2);
			base.WriteTo(writer);
			writer.Write(_insignificant3);
			writer.Write(_terminator);
		}

		/// <summary>
		/// Formats this GroupedField using the specified formatter.
		/// </summary>
		public override void Format(IFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");

			formatter.Format(this, ref _insignificant1, ref _insignificant2, ref _insignificant3, ref _terminator);

			if(_insignificant1 == null || !string.IsNullOrWhiteSpace(_insignificant1))
				throw new InvalidOperationException("insignificant1 must only contain whitespace characters.");
			if(_insignificant2 == null || !string.IsNullOrWhiteSpace(_insignificant2))
				throw new InvalidOperationException("insignificant2 must only contain whitespace characters.");
			if(_insignificant3 == null || !string.IsNullOrWhiteSpace(_insignificant3))
				throw new InvalidOperationException("insignificant3 must only contain whitespace characters.");
			if(_terminator == null || (_terminator != ";" && _terminator != "," && _terminator != ""))
				throw new InvalidOperationException("terminator must be a semicolon, a comma, or an empty string");
			if(!HasTerminator && !IsLastLocalNodeInParent)
				throw new InvalidOperationException("Terminator not specified but was required.");
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Sets the parent of this GroupedField.
		/// </summary>
		internal override void SetParent(Parent parent)
		{
			base.SetParent(parent);
			_parent = (Group)parent;
		}

		/// <summary>
		/// Gets or sets whether this node has a terminating semicolon or comma.
		/// </summary>
		bool IGroupedNodeEx.HasTerminator
		{
			get { return HasTerminator; }
			set { HasTerminator = value; }
		}

		/// <summary>
		/// Gets or sets whether this node has a terminating semicolon or comma.
		/// </summary>
		internal bool HasTerminator
		{
			get { return _terminator != ""; }
			set
			{
				if(value != (_terminator != ""))
					_terminator = value ? ";" : "";
			}
		}

		/// <summary>
		/// Returns the hash tag for this node, or null if it is not hash-tagged.
		/// </summary>
		protected override string GetHashTag()
		{
			if(HasHashTag)
				return "#" + _name;
			else
				return null;
		}

		/// <summary>
		/// Returns whether we should stop reading at the specified token.
		/// </summary>
		private static bool StopRule(Token token)
		{
			return token.Text == ";" || token.Text == ",";
		}

		#endregion
		#region Event Invokers

		/// <summary>
		/// Invokes the NameChanged event.
		/// </summary>
		private void InvokeNameChanged()
		{
			if(NameChanged != null)
				NameChanged(this, EventArgs.Empty);
		}

		#endregion
	}
}