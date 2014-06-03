using System;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A Dummy node contained by a Group node.
	/// </summary>
	public class GroupedDummy : Dummy, IGroupedNodeEx
	{
		#region Events

		/// <summary>
		/// Invoked when this GroupedDummy's name has changed.
		/// </summary>
		public event EventHandler<EventArgs> NameChanged;

		#endregion
		#region Private Fields

		private Group _parent;

		private string _name;
		private string _insignificant1;
		private string _terminator;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the Group that contains this GroupedDummy.
		/// </summary>
		public new Group Parent
		{
			get { return _parent; }
		}

		/// <summary>
		/// Gets or sets the name of this GroupedDummy.
		/// </summary>
		public new string Name
		{
			get { return _name; }
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");

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
		/// Parses a new GroupedDummy.
		/// </summary>
		/// <param name="parent">The parent Group of this GroupedDummy.</param>
		/// <param name="name">Name token of this dummy.</param>
		/// <param name="insignificant1">Insignificant text between name token and semicolon.</param>
		/// <param name="optionalTerminator">Optional semicolon.</param>
		internal GroupedDummy(Group parent, Token name, Token insignificant1, Token optionalTerminator)
			: base(parent)
		{
			_parent = parent;

			// Validate tokens.
			if(!name.IsIdentifier())
				throw new Exception("Internal error: The specified name token is not an identifier.");
			if(!insignificant1.IsInsignificant())
				throw new Exception("Internal error: The specified insignificant1 token is not insignificant.");
			if(optionalTerminator.Text != "" && optionalTerminator.Text != ";" && optionalTerminator.Text != ",")
				throw new Exception( "Internal error: The specified optional semicolon token neither a semicolon nor an empty string.");

			// Set tokens.
			_name = name.Text.Unhashed();
			_insignificant1 = insignificant1.Text;
			_terminator = optionalTerminator.Text;
			HasHashTag = name.IsHashTagged();
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this GroupedDummy.
		/// </summary>
		public override string GetDefiningText()
		{
			return (HasHashTag ? "#" : "") + _name + _insignificant1 + _terminator;
		}

		/// <summary>
		/// Gets text that defines this GroupedDummy excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			return (HasHashTag ? "#" : "") + _name + _terminator;
		}

		/// <summary>
		/// Writes the text that defines this GroupedDummy to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			if(HasHashTag)
				writer.Write('#');
			writer.Write(_name);
			writer.Write(_insignificant1);
			writer.Write(_terminator);
		}

		/// <summary>
		/// Formats this GroupedDummy using the specified formatter.
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
		/// Sets the parent of this GroupedDummy.
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