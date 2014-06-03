using System;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Exposes methods belonging to a node contained in a group.
	/// </summary>
	public interface IGroupedNode : INode
	{
		#region Events

		/// <summary>
		/// Invoked when the node's name has changed.
		/// </summary>
		event EventHandler<EventArgs> NameChanged;

		#endregion
		#region Methods

		/// <summary>
		/// Gets the parent Group of this node.
		/// </summary>
		new Group Parent { get; }

		/// <summary>
		/// Gets or sets the name of this node.
		/// </summary>
		new string Name { get; set; }

		/// <summary>
		/// Gets or sets whether the name of this node has been hash-tagged and can thus be accessed with "#name" from anywhere.
		/// </summary>
		new bool HasHashTag { get; set; }

		#endregion
	}

	/// <summary>
	/// Extends the IGroupedNode and INodeEx interfaces with additional internal members.
	/// </summary>
	internal interface IGroupedNodeEx : IGroupedNode, INodeEx
	{
		/// <summary>
		/// Gets or sets whether this node has a terminating semicolon or comma.
		/// </summary>
		bool HasTerminator { get; set; }
	}
}