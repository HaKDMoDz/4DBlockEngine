using System;
using System.Collections;
using System.Collections.Generic;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A node which stores a number of child nodes.
	/// </summary>
	public abstract class Parent : Node, IEnumerable<INode>
	{
		#region Properties

		/// <summary>
		/// Gets the number of nodes in this Parent including inherited nodes.
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Gets the node with the specified name or string-formatted index including inherited nodes.
		/// </summary>
		public INode this[string name]
		{
			get
			{
				if(name == null)
					throw new ArgumentNullException("name");

				return GetNodeNamed(name);
			}
		}

		/// <summary>
		/// Gets the node at the specified index including inherited nodes.
		/// </summary>
		public INode this[int index]
		{
			get { return GetNodeAt(index); }
		}

		/// <summary>
		/// Gets the collection of nodes contained locally in this Parent excluding inherited nodes.
		/// </summary>
		public ChildCollection LocalChildren
		{
			get { return GetLocalChildrenCollection(); }
		}

		/// <summary>
		/// Gets all of the names used by nodes including inherited nodes in this Parent.
		/// </summary>
		public abstract IEnumerable<string> Names { get; }

		#endregion
		#region Constructors

		/// <summary>
		/// Initializes a new Parent.
		/// </summary>
		internal Parent(Parent parent)
			: base(parent)
		{
			// Do nothing.
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this Parent's body.
		/// </summary>
		public abstract string GetBodyText();

		/// <summary>
		/// Returns whether this Parent contains the specified node including inherited nodes.
		/// </summary>
		public abstract bool Contains(INode node);

		/// <summary>
		/// Returns whether this Parent contains a node with the specified name including inherited nodes.
		/// </summary>
		public abstract bool Contains(string name);

		/// <summary>
		/// Returns the name or string-formatted index of the specified node including inherited nodes or null if it is not found.
		/// </summary>
		public abstract string NameOf(INode node);

		/// <summary>
		/// Returns the index of the specified node including inherited nodes or -1 if it is not found.
		/// </summary>
		public abstract int IndexOf(INode node);

		/// <summary>
		/// Attempts to get a node with the specified name including inherited nodes.
		/// </summary>
		/// <param name="node">The found node, or null if no node was found.</param>
		/// <returns>Whether a node with the specified name was found.</returns>
		public abstract bool TryGetNode(string name, out INode node);

		/// <summary>
		/// Copies the nodes including inherited nodes in this Parent to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this Parent.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		public abstract void CopyTo(INode[] array, int arrayIndex);

		/// <summary>
		/// Gets an enumerator which iterates through the nodes including inherited nodes in this Parent.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Gets an enumerator which iterates through the nodes including inherited nodes in this Parent.
		/// </summary>
		public IEnumerator<INode> GetEnumerator()
		{
			return GetLocalEnumerator();
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Gets an enumerator which iterates through the local nodes excluding inherited nodes in this Parent.
		/// </summary>
		protected abstract IEnumerator<INode> GetLocalEnumerator();

		/// <summary>
		/// Gets the node with the specified name including inherited nodes.
		/// </summary>
		protected abstract INode GetNodeNamed(string name);

		/// <summary>
		/// Gets the node at the specified index including inherited nodes.
		/// </summary>
		protected abstract INode GetNodeAt(int index);

		/// <summary>
		/// Gets the collection of nodes contained locally in this Parent excluding inherited nodes.
		/// </summary>
		protected abstract ChildCollection GetLocalChildrenCollection();

		#endregion
	}
}