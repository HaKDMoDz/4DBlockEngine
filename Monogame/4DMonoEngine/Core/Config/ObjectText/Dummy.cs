using System;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A node which does not store any information but simply acts as a placeholder.
	/// </summary>
	public abstract class Dummy : Node
	{
		#region Constructors

		/// <summary>
		/// Initializes a new Dummy.
		/// </summary>
		internal Dummy(Parent parent)
			: base(parent)
		{
			// Do nothing.
		}

		#endregion
		#region Public Static Methods

		/// <summary>
		/// Replaces the specified old node with a new Dummy node.
		/// </summary>
		public static Dummy Replace(INode oldNode)
		{
			if(oldNode == null)
				throw new ArgumentNullException("oldNode");

			if(!(oldNode is Dummy))
			{
				if(oldNode is IGroupedNode)
					return (Dummy)oldNode.Parent.LocalChildren.Replace(oldNode, oldNode.Name + ";")[0];
				else if(oldNode is IListedNode)
					return (Dummy)oldNode.Parent.LocalChildren.Replace(oldNode, "?")[0];
				else
					throw new Exception("Internal error: Unable to determine type of oldNode.");
			}
			else
			{
				return (Dummy)oldNode;
			}
		}

		#endregion
	}
}