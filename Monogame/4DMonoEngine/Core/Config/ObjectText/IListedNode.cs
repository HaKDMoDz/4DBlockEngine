namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Exposes methods belonging to a node contained in a list.
	/// </summary>
	public interface IListedNode : INode
	{
		/// <summary>
		/// Gets the parent List of this node.
		/// </summary>
		new List Parent { get; }
	}

	/// <summary>
	/// Extends the IListedNode and INodeEx interface with additional internal members.
	/// </summary>
	internal interface IListedNodeEx : IListedNode, INodeEx
	{
		/// <summary>
		/// Gets or sets whether this node has a terminating comma or semicolon.
		/// </summary>
		bool HasTerminator { get; set; }
	}
}