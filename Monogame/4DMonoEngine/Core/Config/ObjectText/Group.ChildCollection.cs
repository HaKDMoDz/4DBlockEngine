using System;
using System.Collections.Generic;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	public partial class Group
	{
		#region Public Types

		/// <summary>
		/// A collection of nodes stored locally in a Group.
		/// </summary>
		public class ChildCollection : ObjectText.ChildCollection, IList<IGroupedNode>, IDictionary<string, IGroupedNode>
		{
			#region Private Fields

			private readonly Group _owner;
			private bool _suppressCountChanged;

			#endregion
			#region Constructors

			/// <summary>
			/// Creates a new ChildCollection.
			/// </summary>
			internal ChildCollection(Group owner)
				: base(owner)
			{
				if(owner == null)
					throw new ArgumentNullException("owner");

				_owner = owner;
			}

			#endregion
			#region Properties

			/// <summary>
			/// Gets the number of nodes in this ChildCollection.
			/// </summary>
			public override sealed int Count
			{
				get { return _owner._children.Count; }
			}

			/// <summary>
			/// Gets whether this ChildCollection is read-only. (always false)
			/// </summary>
			bool ICollection<IGroupedNode>.IsReadOnly
			{
				get { return false; }
			}

			/// <summary>
			/// Gets whether this ChildCollection is read-only. (always false)
			/// </summary>
			bool ICollection<KeyValuePair<string, IGroupedNode>>.IsReadOnly
			{
				get { return false; }
			}

			/// <summary>
			/// Gets the node at the specified index.
			/// </summary>
			public new IGroupedNode this[int index]
			{
				get { return _owner._children[index]; }
			}

			/// <summary>
			/// Gets or sets the node at the specified index.
			/// </summary>
			IGroupedNode IList<IGroupedNode>.this[int index]
			{
				get { return this[index]; }
				set { ReplaceAt(index, value.GetDefiningText()); }
			}

			/// <summary>
			/// Gets or sets the node of the specified name.
			/// </summary>
			IGroupedNode IDictionary<string, IGroupedNode>.this[string name]
			{
				get { return this[name]; }
				set
				{
					if(value.Name != name)
						throw new KeyNotFoundException("The name of the specified node must match the specified name.");

					int index = IndexOf(this[name]);
					ReplaceAt(index, value.GetDefiningText());
				}
			}

			/// <summary>
			/// Gets the node with the specified name.
			/// </summary>
			public new IGroupedNode this[string name]
			{
				get { return _owner._childrenByName[name.ToLower()]; }
			}

			/// <summary>
			/// Gets a collection containing all of the names used by nodes in this ChildCollection.
			/// </summary>
			ICollection<string> IDictionary<string, IGroupedNode>.Keys
			{
				get { return Names; }
			}

			/// <summary>
			/// Gets a collection containing all of the names used by the nodes in this ChildCollection.
			/// </summary>
			public ICollection<string> Names
			{
				get { return _owner._childrenByName.Keys; }
			}

			/// <summary>
			/// Gets a collection containing all of the nodes in this ChildCollection.
			/// </summary>
			ICollection<IGroupedNode> IDictionary<string, IGroupedNode>.Values
			{
				get { return this; }
			}

			/// <summary>
			/// Gets the Group that owns this ChildCollection.
			/// </summary>
			public new Group Owner
			{
				get { return _owner; }
			}

			#endregion
			#region Public Methods

			/// <summary>
			/// Adds a copy of the specified node to the end of this ChildCollection.
			/// </summary>
			void ICollection<IGroupedNode>.Add(IGroupedNode node)
			{
				if(node == null)
					throw new ArgumentNullException("node");

				Add(node.GetDefiningText());
			}

			/// <summary>
			/// Adds a copy of the specified node to the end of this ChildCollection.
			/// </summary>
			void IDictionary<string, IGroupedNode>.Add(string name, IGroupedNode node)
			{
				if(name == null)
					throw new ArgumentNullException("name");
				if(node == null)
					throw new ArgumentNullException("node");
				if(node.Name != name)
					throw new ArgumentException("The name of the specified node and the specified name do not match.");
				
				Add(node.GetDefiningText());
			}

			/// <summary>
			/// Adds a copy of the specified node to the end of this ChildCollection.
			/// </summary>
			void ICollection<KeyValuePair<string, IGroupedNode>>.Add(KeyValuePair<string, IGroupedNode> item)
			{
				((IDictionary<string, IGroupedNode>)this).Add(item.Key, item.Value);
			}

			/// <summary>
			/// Adds a new node or new nodes defined by the specified text to the end of this ChildCollection.
			/// </summary>
			/// <returns>The added node or nodes.</returns>
			public new IGroupedNode[] Add(string text)
			{
				return Insert(Count, text);
			}

			/// <summary>
			/// Inserts a copy of the specified node at the specified index.
			/// </summary>
			void IList<IGroupedNode>.Insert(int index, IGroupedNode node)
			{
				if(node == null)
					throw new ArgumentNullException("node");

				Insert(index, node.GetDefiningText());
			}

			/// <summary>
			/// Inserts a new node or new nodes defined by the specified text at the specified index.
			/// </summary>
			/// <returns>The inserted node or nodes.</returns>
			public new IGroupedNode[] Insert(int index, string text)
			{
				if(text == null)
					throw new ArgumentNullException("text");

				// Create tokenizer.
				StringStream buf = new StringStream(text);
				BinaryReader reader = new BinaryReader(buf);
				Tokenizer tok = new Tokenizer(reader);

				// Read initial token and handle appropriately if it is insignificant.
				Token t = tok.Read();
				if(t.IsInsignificant())
				{
					if(index == 0)
						_owner.AppendBodyInsignificant(t.Text);
					else
						_owner._insignificants[index - 1] = _owner._insignificants[index - 1] + t;
					t = tok.Read();
				}

				// Parse.
				Token t2;
				int oldCount = Count;
				_owner.Parse(
					t,
					tok,
					token => token.IsEndOfFile,
					index,
					out t2);

				// Invoke event?
				if(oldCount != Count)
					InvokeCountChanged();

				// Return.
				IGroupedNode[] ret = new IGroupedNode[Count - oldCount];
				_owner._children.CopyTo(index, ret, 0, Count - oldCount);
				if(index > 0)
					((IGroupedNodeEx)_owner._children[index - 1]).HasTerminator = true;
				return ret;
			}

			/// <summary>
			/// Removes the specified node from this ChildCollection.
			/// </summary>
			/// <returns>Whether the specified node was succesfully removed.</returns>
			bool ICollection<IGroupedNode>.Remove(IGroupedNode node)
			{
				return Remove(node);
			}

			/// <summary>
			/// Removes the specified node from this ChildCollection.
			/// </summary>
			bool ICollection<KeyValuePair<string, IGroupedNode>>.Remove(KeyValuePair<string, IGroupedNode> item)
			{
				if(Contains(item.Key) && Contains(item.Value) && this[item.Key] == item.Value)
					return Remove(item.Value);
				else
					return false;
			}

			/// <summary>
			/// Removes the specified node from this ChildCollection.
			/// </summary>
			/// <returns>Whether the specified node was succesfully removed.</returns>
			public override sealed bool Remove(INode node)
			{
				IGroupedNode groupedNode = node as IGroupedNode;
				if(groupedNode == null)
					return false;
				int i = _owner._children.IndexOf(groupedNode);
				if(i != -1)
				{
					RemoveAt(i);
					return true;
				}
				else
				{
					return false;
				}
			}

			/// <summary>
			/// Removes the node at the specified index.
			/// </summary>
			public override sealed void RemoveAt(int index)
			{
				IGroupedNodeEx groupedNode = (IGroupedNodeEx)this[index];
				groupedNode.SetParent(null);
				_owner._children.RemoveAt(index);
				_owner._childrenByName.Remove(groupedNode.Name.ToLower());
				_owner._insignificants.RemoveAt(index);
				InvokeCountChanged();
			}

			/// <summary>
			/// Replaces the specified old node with a new node or nodes defined by the specified text.
			/// </summary>
			/// <returns>The new node or nodes.</returns>
			public new IGroupedNode[] Replace(INode oldNode, string newText)
			{
				if(oldNode == null)
					throw new ArgumentNullException("oldNode");
				if(newText == null)
					throw new ArgumentNullException("newText");
				if(!Contains(oldNode))
					throw new InvalidOperationException("The specified Group does not contain the specified node.");

				return ReplaceRange(IndexOf(oldNode), 1, newText);
			}

			/// <summary>
			/// Replaces the nodes in the specified range with a new node or nodes defined by the specified text.
			/// </summary>
			/// <returns>The new node or nodes.</returns>
			public new IGroupedNode[] ReplaceRange(int index, int count, string newText)
			{
				if(newText == null)
					throw new ArgumentNullException("newText");

				int oldCount = Count;
				_suppressCountChanged = true;

				// Remove old nodes.
				for(int i = 0; i < count; i++)
					RemoveAt(index);

				// Insert new text.
				IGroupedNode[] ret = Insert(index, newText);

				_suppressCountChanged = false;

				// Invoke event?
				if(oldCount != Count)
					InvokeCountChanged();
				InvokeOrderChanged();

				return ret;
			}

			/// <summary>
			/// Removes all nodes from this ChildCollection.
			/// </summary>
			public override sealed void Clear()
			{
				if(Count > 0)
				{
					_owner._children.Clear();
					_owner._childrenByName.Clear();
					_owner._insignificants.Clear();
					InvokeCountChanged();
				}
			}

			/// <summary>
			/// Reverses the order of all the nodes in this ChildCollection.
			/// </summary>
			public override sealed void Reverse()
			{
				Reverse(0, Count);
			}

			/// <summary>
			/// Reverses the order of the nodes in the specified range.
			/// </summary>
			public override sealed void Reverse(int index, int count)
			{
				if(count > 1)
				{
					_owner._children.Reverse(index, count);
					InvokeOrderChanged();
				}
			}

			/// <summary>
			/// Sorts the nodes in this ChildCollection using the specified comparison.
			/// </summary>
			public override sealed void Sort(Comparison<INode> comparison)
			{
				_owner._children.Sort(comparison);
				InvokeOrderChanged();
			}

			/// <summary>
			/// Sorts the nodes in this ChildCollection using the specified comparer.
			/// </summary>
			public override sealed void Sort(Comparer<INode> comparer)
			{
				_owner._children.Sort(comparer);
				InvokeOrderChanged();
			}

			/// <summary>
			/// Sorts the nodes in this ChildCollection using the specified comparison.
			/// </summary>
			public void Sort(Comparison<IGroupedNode> comparison)
			{
				_owner._children.Sort(comparison);
				InvokeOrderChanged();
			}

			/// <summary>
			/// Sorts the nodes in this ChildCollection using the specified comparer.
			/// </summary>
			public void Sort(IComparer<IGroupedNode> comparer)
			{
				_owner._children.Sort(comparer);
				InvokeOrderChanged();
			}

			/// <summary>
			/// Returns whether this ChildCollection contains the specified node.
			/// </summary>
			bool ICollection<IGroupedNode>.Contains(IGroupedNode node)
			{
				return _owner._children.Contains(node);
			}

			/// <summary>
			/// Returns whether this ChildCollection contains a node by the speciifed name.
			/// </summary>
			bool ICollection<KeyValuePair<string, IGroupedNode>>.Contains(KeyValuePair<string, IGroupedNode> item)
			{
				IGroupedNode found;
				return _owner._childrenByName.TryGetValue(item.Key.ToLower(), out found) && found == item.Value;
			}

			/// <summary>
			/// Returns whether this ChildCollection contains the specified node.
			/// </summary>
			public override sealed bool Contains(INode node)
			{
				IGroupedNode groupedNode = node as IGroupedNode;
				if(groupedNode != null)
					return _owner._children.Contains(groupedNode);
				else
					return false;
			}

			/// <summary>
			/// Returns whether this ChildCollection contains a node with the specified name.
			/// </summary>
			bool IDictionary<string, IGroupedNode>.ContainsKey(string name)
			{
				return Contains(name);
			}

			/// <summary>
			/// Returns whether this ChildCollection contains a node with the specified name.
			/// </summary>
			public override sealed bool Contains(string name)
			{
				return _owner._childrenByName.ContainsKey(name.ToLower());
			}

			/// <summary>
			/// Returns the name of the specified node or null if this ChildCollection does not contain the specified node.
			/// </summary>
			public override sealed string NameOf(INode node)
			{
				if(Contains(node))
					return node.Name;
				else
					return null;
			}

			/// <summary>
			/// Returns the index of the specified node or -1 if it is not found.
			/// </summary>
			int IList<IGroupedNode>.IndexOf(IGroupedNode node)
			{
				return _owner._children.IndexOf(node);
			}

			/// <summary>
			/// Returns the index of the specified node or -1 if it is not found.
			/// </summary>
			public override sealed int IndexOf(INode node)
			{
				IGroupedNode groupedNode = node as IGroupedNode;
				if(groupedNode != null)
					return _owner._children.IndexOf(groupedNode);
				else
					return -1;
			}

			/// <summary>
			/// Attempts to get a node with the specified name.
			/// </summary>
			/// <param name="node">The found node, or null if no node was found.</param>
			/// <returns>Whether a node with the specified name was found.</returns>
			public override sealed bool TryGetNode(string name, out INode node)
			{
				IGroupedNode outNode;
				bool ret = TryGetNode(name, out outNode);
				node = outNode;
				return ret;
			}

			/// <summary>
			/// Attempts to get a node with the specified name.
			/// </summary>
			/// <param name="node">The found node, or null if no node was found.</param>
			/// <returns>Whether a node with the specified name was found.</returns>
			public bool TryGetNode(string name, out IGroupedNode node)
			{
				return _owner._childrenByName.TryGetValue(name.ToLower(), out node);
			}

			/// <summary>
			/// Copies the nodes and names in this ChildCollection to an array.
			/// </summary>
			/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this ChildCollection.</param>
			/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
			void ICollection<KeyValuePair<string, IGroupedNode>>.CopyTo(KeyValuePair<string, IGroupedNode>[] array, int arrayIndex)
			{
				((IDictionary<string, IGroupedNode>)_owner._childrenByName).CopyTo(array, arrayIndex);
			}

			/// <summary>
			/// Copies the nodes in this ChildCollection to an array.
			/// </summary>
			/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this ChildCollection.</param>
			/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
			public override sealed void CopyTo(INode[] array, int arrayIndex)
			{
				if(array == null)
					throw new ArgumentNullException("array");

				for(int i = 0; i < Count; i++)
					array[i + arrayIndex] = this[i];
			}

			/// <summary>
			/// Copies the nodes in this ChildCollection to an array.
			/// </summary>
			/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this ChildCollection.</param>
			/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
			public void CopyTo(IGroupedNode[] array, int arrayIndex)
			{
				_owner._children.CopyTo(array);
			}

			/// <summary>
			/// Attempts to retrieve a node with the specified name.
			/// </summary>
			/// <returns>Whether a node of the specified name was found.</returns>
			bool IDictionary<string, IGroupedNode>.TryGetValue(string name, out IGroupedNode node)
			{
				return TryGetNode(name, out node);
			}

			/// <summary>
			/// Gets an enumerator which iterates through the nodes and names in this ChildCollection.
			/// </summary>
			IEnumerator<KeyValuePair<string, IGroupedNode>> IEnumerable<KeyValuePair<string, IGroupedNode>>.GetEnumerator()
			{
				return _owner._childrenByName.GetEnumerator();
			}

			/// <summary>
			/// Gets an enumerator which iterates through the nodes in this ChildCollection.
			/// </summary>
			public new IEnumerator<IGroupedNode> GetEnumerator()
			{
				return _owner._children.GetEnumerator();
			}

			#endregion
			#region Non-Public Methods

			/// <summary>
			/// Throws an exception if the specified node is not compatible with this ChildCollection.
			/// </summary>
			protected override sealed void ThrowOnNodeIncompatibility(INode node)
			{
				if(!(node is IGroupedNode))
					throw new InvalidOperationException("The specified node is not a grouped node.");
			}

			/// <summary>
			/// Adds a new node or new nodes defined by the specified text to the end of this ChildCollection.
			/// </summary>
			/// <param name="text">An array containing the added node or nodes.</param>
			protected override sealed INode[] AddNodeText(string text)
			{
				return Array.ConvertAll<IGroupedNode, INode>(Add(text), gn => gn);
			}

			/// <summary>
			/// Inserts a new node or new nodes defined by the specified text at the specified index.
			/// </summary>
			/// <returns>An array containing the inserted node or nodes.</returns>
			protected override sealed INode[] InsertNodeText(int index, string text)
			{
				return Array.ConvertAll<IGroupedNode, INode>(Insert(index, text), gn => gn);
			}

			/// <summary>
			/// Replaces the specified old node with a new node or nodes defined by the specified text.
			/// </summary>
			protected override sealed INode[] ReplaceWithNodeText(INode oldNode, string newText)
			{
				return Array.ConvertAll<IGroupedNode, INode>(Replace(oldNode, newText), gn => gn);
			}

			/// <summary>
			/// Replaces the nodes in the specified range with a new node or nodes defined by the specified text.
			/// </summary>
			protected override sealed INode[] ReplaceRangeWithNodeText(int index, int count, string newText)
			{
				return Array.ConvertAll<IGroupedNode, INode>(ReplaceRange(index, count, newText), gn => gn);
			}

			/// <summary>
			/// Gets an enumerator which iterates through the nodes in this ChildCollection.
			/// </summary>
			protected override sealed IEnumerator<INode> GetINodeEnumerator()
			{
				return _owner._children.GetEnumerator();
			}

			/// <summary>
			/// Gets the node with the specified name.
			/// </summary>
			protected override sealed INode GetNodeNamed(string name)
			{
				return this[name];
			}

			/// <summary>
			/// Gets the node at the specified index.
			/// </summary>
			protected override sealed INode GetNodeAt(int index)
			{
				return this[index];
			}

			#endregion
			#region Event Invokers

			/// <summary>
			/// Invokes the CountChanged event.
			/// </summary>
			private new void InvokeCountChanged()
			{
				if(!_suppressCountChanged)
					base.InvokeCountChanged();
			}

			#endregion
		}

		#endregion
	}
}