using System;
using System.Collections;
using System.Collections.Generic;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// The base class for a collection of nodes stored locally in a Parent node.
	/// </summary>
	public abstract class ChildCollection : IList<INode>
	{
		#region Events

		/// <summary>
		/// Invoked when the number of child nodes has changed.
		/// </summary>
		public event EventHandler<EventArgs> CountChanged;

		/// <summary>
		/// Invoked when the order of child nodes has changed.
		/// </summary>
		public event EventHandler<EventArgs> OrderChanged;

		#endregion
		#region Private Fields

		private readonly Parent _owner;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the number of nodes in this ChildCollection.
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Gets whether this ChildCollection is read-only. (always false)
		/// </summary>
		bool ICollection<INode>.IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Gets or sets the node at the specified index.
		/// </summary>
		INode IList<INode>.this[int index]
		{
			get { return this[index]; }
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");

				ThrowOnNodeIncompatibility(value);
				RemoveAt(index);
				Insert(index, value.GetDefiningText());
			}
		}

		/// <summary>
		/// Gets the node with the specified name or string-formatted index.
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
		/// Gets the node at the specified index.
		/// </summary>
		public INode this[int index]
		{
			get { return GetNodeAt(index); }
		}

		/// <summary>
		/// Gets the owner of this ChildCollection.
		/// </summary>
		public Parent Owner
		{
			get { return _owner; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Initializes a new ChildCollection.
		/// </summary>
		internal ChildCollection(Parent owner)
		{
			if(owner == null)
				throw new ArgumentNullException("owner");

			_owner = owner;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Adds a copy of the specified node to the end of this ChildCollection.
		/// </summary>
		void ICollection<INode>.Add(INode node)
		{
			if(node == null)
				throw new ArgumentNullException("node");

			ThrowOnNodeIncompatibility(node);
			Add(node.GetDefiningText());
		}

		/// <summary>
		/// Adds a new node or new nodes defined by the specified text to the end of this ChildCollection.
		/// </summary>
		/// <returns>The added node or nodes.</returns>
		public INode[] Add(string text)
		{
			if(text == null)
				throw new ArgumentNullException("text");

			return AddNodeText(text);
		}

		/// <summary>
		/// Inserts a copy of the specified node at the specified index.
		/// </summary>
		void IList<INode>.Insert(int index, INode node)
		{
			if(node == null)
				throw new ArgumentNullException("node");

			ThrowOnNodeIncompatibility(node);
			Insert(index, node.GetDefiningText());
		}

		/// <summary>
		/// Inserts a new node or new nodes defined by the specified text at the specified index.
		/// </summary>
		/// <returns>The inserted node or nodes.</returns>
		public INode[] Insert(int index, string text)
		{
			if(text == null)
				throw new ArgumentNullException("text");

			return InsertNodeText(index, text);
		}

		/// <summary>
		/// Removes the specified node from this ChildCollection.
		/// </summary>
		/// <returns>Whether the specified node was succesfully removed.</returns>
		public abstract bool Remove(INode node);

		/// <summary>
		/// Removes the node with the specified name.
		/// </summary>
		public bool Remove(string name)
		{
			if(name == null)
				throw new ArgumentNullException("name");

			if(Contains(name))
				return Remove(this[name]);
			else
				return false;
		}

		/// <summary>
		/// Removes the node at the specified index.
		/// </summary>
		public abstract void RemoveAt(int index);

		/// <summary>
		/// Replaces the specified old node with a new node or nodes defined by the specified text.
		/// </summary>
		/// <returns>The new node or nodes.</returns>
		public INode[] Replace(INode oldNode, string newText)
		{
			if(oldNode == null)
				throw new ArgumentNullException("oldNode");
			if(newText == null)
				throw new ArgumentNullException("newText");

			return ReplaceWithNodeText(oldNode, newText);
		}

		/// <summary>
		/// Replaces the node with the specified name with a new node or nodes defined by the specified text.
		/// </summary>
		/// <returns>The new node or nodes.</returns>
		public INode[] Replace(string oldName, string newText)
		{
			if(newText == null)
				throw new ArgumentNullException("newText");

			return Replace(this[oldName], newText);
		}

		/// <summary>
		/// Replaces the node at the specified index with a new node or nodes defined by the specified text.
		/// </summary>
		/// <returns>The new node or nodes.</returns>
		public INode[] ReplaceAt(int index, string newText)
		{
			return ReplaceRange(index, 1, newText);
		}

		/// <summary>
		/// Replaces the nodes in the specified range with a new node or nodes defined by the specified text.
		/// </summary>
		/// <returns>The new node or nodes.</returns>
		public INode[] ReplaceRange(int index, int count, string newText)
		{
			if(newText == null)
				throw new ArgumentNullException("newText");

			return ReplaceRangeWithNodeText(index, count, newText);
		}

		/// <summary>
		/// Removes all nodes from this ChildCollection.
		/// </summary>
		public abstract void Clear();

		/// <summary>
		/// Reverses the order of the nodes in this ChildCollection.
		/// </summary>
		public abstract void Reverse();

		/// <summary>
		/// Reverses the order of the nodes in the specified range.
		/// </summary>
		public abstract void Reverse(int index, int count);

		/// <summary>
		/// Sorts the nodes in this ChildCollection using the specified comparison.
		/// </summary>
		public abstract void Sort(Comparison<INode> comparison);

		/// <summary>
		/// Sorts the nodes in this ChildCollection using the specified comparer.
		/// </summary>
		public abstract void Sort(Comparer<INode> comparer);

		/// <summary>
		/// Returns whether this ChildCollection contains the specified node.
		/// </summary>
		public abstract bool Contains(INode node);

		/// <summary>
		/// Returns whether this ChildCollection contains a node with the specified name.
		/// </summary>
		public abstract bool Contains(string name);

		/// <summary>
		/// Returns the name or string-formatted index of the specified node or null if it is not found.
		/// </summary>
		public abstract string NameOf(INode node);

		/// <summary>
		/// Returns the index of the specified node or -1 if it is not found.
		/// </summary>
		public abstract int IndexOf(INode node);

		/// <summary>
		/// Attempts to get a node with the specified name.
		/// </summary>
		/// <param name="node">The found node, or null if no node was found.</param>
		/// <returns>Whether a node with the specified name was found.</returns>
		public abstract bool TryGetNode(string name, out INode node);

		/// <summary>
		/// Copies the nodes in this ChildCollection to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this Parent.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		public abstract void CopyTo(INode[] array, int arrayIndex);

		/// <summary>
		/// Gets an enumerator which iterates through the nodes in this ChildCollection.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Gets an enumerator which iterates through the nodes in this ChildCollection.
		/// </summary>
		public IEnumerator<INode> GetEnumerator()
		{
			return GetINodeEnumerator();
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Throws an exception if the specified node is not compatible with this ChildCollection.
		/// </summary>
		protected abstract void ThrowOnNodeIncompatibility(INode node);

		/// <summary>
		/// Adds a new node or new nodes defined by the specified text to the end of this ChildCollection.
		/// </summary>
		/// <returns>The added node or nodes.</returns>
		protected abstract INode[] AddNodeText(string text);

		/// <summary>
		/// Inserts a new node or new nodes defined by the specified text at the specified index.
		/// </summary>
		/// <returns>The inserted node or nodes.</returns>
		protected abstract INode[] InsertNodeText(int index, string text);

		/// <summary>
		/// Replaces the specified old node with a new node or nodes defined by the specified text.
		/// </summary>
		/// <returns>The new node or nodes.</returns>
		protected abstract INode[] ReplaceWithNodeText(INode oldNode, string newText);

		/// <summary>
		/// Replaces the nodes in the specified range with a new node or nodes defined by the specified text.
		/// </summary>
		/// <returns>The new node or nodes.</returns>
		protected abstract INode[] ReplaceRangeWithNodeText(int index, int count, string newText);

		/// <summary>
		/// Gets an enumerator which iterates through the nodes in this ChildCollection.
		/// </summary>
		protected abstract IEnumerator<INode> GetINodeEnumerator();

		/// <summary>
		/// Gets the node with the specified name.
		/// </summary>
		protected abstract INode GetNodeNamed(string name);

		/// <summary>
		/// Gets the node at the specified index.
		/// </summary>
		protected abstract INode GetNodeAt(int index);

		#endregion
		#region Event Invokers

		/// <summary>
		/// Invokes the CountChanged event.
		/// </summary>
		protected void InvokeCountChanged()
		{
			if(CountChanged != null)
				CountChanged(this, EventArgs.Empty);
		}

		/// <summary>
		/// Invokes the OrderChanged event.
		/// </summary>
		protected void InvokeOrderChanged()
		{
			if(OrderChanged != null)
				OrderChanged(this, EventArgs.Empty);
		}

		#endregion
	}
}