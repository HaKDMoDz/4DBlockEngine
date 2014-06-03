using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	public partial class InheritenceList
	{
		#region Public Types

		/// <summary>
		/// A collection of InheritenceReferences stored in an InheritenceList.
		/// </summary>
		public class ChildCollection : ObjectText.ChildCollection, IList<InheritenceReference>
		{
			#region Private Fields

			private readonly InheritenceList _owner;
			private bool _suppressCountChanged;

			#endregion
			#region Constructors

			/// <summary>
			/// Creates a new ChildCollection.
			/// </summary>
			internal ChildCollection(InheritenceList owner)
				: base(owner)
			{
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
			bool ICollection<InheritenceReference>.IsReadOnly
			{
				get { return false; }
			}

			/// <summary>
			/// Gets the node at the specified string-formatted index.
			/// </summary>
			public new InheritenceReference this[string name]
			{
				get
				{
					InheritenceReference ir;
					if(TryGetNode(name, out ir))
						return ir;
					else
						throw new KeyNotFoundException("The specified InheritenceList does not contain a node by the specified name.");
				}
			}

			/// <summary>
			/// Gets or sets the node at the specified index.
			/// </summary>
			InheritenceReference IList<InheritenceReference>.this[int index]
			{
				get { return this[index]; }
				set { ReplaceAt(index, value.GetDefiningText()); }
			}

			/// <summary>
			/// Gets the node at the specified index.
			/// </summary>
			public new InheritenceReference this[int index]
			{
				get
				{
					if(index < 0 || index > Count)
						throw new ArgumentOutOfRangeException("index");
					return _owner._children[index];
				}
			}

			/// <summary>
			/// Gets the owner of this ChildCollection.
			/// </summary>
			public new InheritenceList Owner
			{
				get { return _owner; }
			}

			#endregion
			#region Public Methods

			/// <summary>
			/// Adds a copy of the specified node to the end of this ChildCollection.
			/// </summary>
			void ICollection<InheritenceReference>.Add(InheritenceReference node)
			{
				Add(node.GetDefiningText());
			}

			/// <summary>
			/// Adds a new node or new nodes defined by the specified text to the end of this ChildCollection.
			/// </summary>
			/// <returns>The added node or nodes.</returns>
			public new InheritenceReference[] Add(string text)
			{
				return Insert(Count, text);
			}

			/// <summary>
			/// Inserts a copy of the specified node at the specified index.
			/// </summary>
			void IList<InheritenceReference>.Insert(int index, InheritenceReference node)
			{
				Insert(index, node.GetDefiningText());
			}

			/// <summary>
			/// Inserts a new node or new nodes defined by the specified text at the specified index.
			/// </summary>
			/// <returns>The inserted node or nodes.</returns>
			public new InheritenceReference[] Insert(int index, string text)
			{
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
				_owner.Parse(t, tok, token => token.IsEndOfFile, index, out t2);

				// Invoke event?
				if(oldCount != Count)
					InvokeCountChanged();

				// Return.
				InheritenceReference[] ret = new InheritenceReference[Count - oldCount];
				_owner._children.CopyTo(index, ret, 0, Count - oldCount);
				if(index > 0)
					_owner._children[index - 1].HasTerminator = true;
				return ret;
			}

			/// <summary>
			/// Removes the specified node from this ChildCollection.
			/// </summary>
			/// <returns>Whether the specified node was succesfully removed.</returns>
			bool ICollection<InheritenceReference>.Remove(InheritenceReference node)
			{
				return Remove(node);
			}

			/// <summary>
			/// Removes the specified node from this ChildCollection.
			/// </summary>
			/// <returns>Whether the specified node was succesfully removed.</returns>
			public override sealed bool Remove(INode node)
			{
				InheritenceReference ir = node as InheritenceReference;
				if(ir == null)
					return false;
				int i = _owner._children.IndexOf(ir);
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
				InheritenceReference ir = this[index];
				ir.SetParent(null);
				_owner._children.RemoveAt(index);
				_owner._insignificants.RemoveAt(index);
				InvokeCountChanged();
			}

			/// <summary>
			/// Replaces the specfiied old node with a new node or nodes defined by the specified text.
			/// </summary>
			/// <returns>The new node or nodes.</returns>
			public new InheritenceReference[] Replace(INode oldNode, string newText)
			{
				if(!Contains(oldNode))
					throw new InvalidOperationException("The specified InheritenceList does not contain the specified node.");

				return ReplaceRange(IndexOf(oldNode), 1, newText);
			}

			/// <summary>
			/// Replaces the nodes in the specified range with a new node or nodes defined by the specified text.
			/// </summary>
			/// <returns>The new node or nodes.</returns>
			public new InheritenceReference[] ReplaceRange(int index, int count, string newText)
			{
				int oldCount = Count;
				_suppressCountChanged = true;

				// Remove old nodes.
				for(int i = 0; i < count; i++)
					RemoveAt(index);

				// Insert new text.
				InheritenceReference[] ret = Insert(index, newText);

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
			public void Sort(Comparison<InheritenceReference> comparison)
			{
				_owner._children.Sort(comparison);
				InvokeOrderChanged();
			}

			/// <summary>
			/// Sorts the nodes in this ChildCollection using the specified comparer.
			/// </summary>
			public void Sort(IComparer<InheritenceReference> comparer)
			{
				_owner._children.Sort(comparer);
				InvokeOrderChanged();
			}

			/// <summary>
			/// Returns whether this ChildCollection contains the specified node.
			/// </summary>
			bool ICollection<InheritenceReference>.Contains(InheritenceReference node)
			{
				return Contains(node);
			}

			/// <summary>
			/// Returns whether this ChildCollection contains the specified node.
			/// </summary>
			public override sealed bool Contains(INode node)
			{
				InheritenceReference ir = node as InheritenceReference;
				if(ir != null)
					return _owner._children.Contains(ir);
				else
					return false;
			}

			/// <summary>
			/// Returns whether this ChildCollection contains a node with the specified string-formatted index.
			/// </summary>
			public override sealed bool Contains(string name)
			{
				int index;
				if(int.TryParse(name, out index))
					return index >= 0 && index < Count;
				else
					return false;
			}

			/// <summary>
			/// Returns whether this ChildCollection contains a Reference with the specified target.
			/// </summary>
			public bool ContainsReference(string target)
			{
				foreach(InheritenceReference ir in this)
				{
					if(ir.Target == target)
						return true;
				}

				return false;
			}

			/// <summary>
			/// Returns the string-formatted index of the specified node or null if it is not found.
			/// </summary>
			public override sealed string NameOf(INode node)
			{
				int index = IndexOf(node);
				if(index != -1)
					return index.ToString(CultureInfo.InvariantCulture);
				else
					return null;
			}

			/// <summary>
			/// Returns the index of the specified node or -1 if it is not found.
			/// </summary>
			int IList<InheritenceReference>.IndexOf(InheritenceReference node)
			{
				return _owner._children.IndexOf(node);
			}

			/// <summary>
			/// Returns the index of the specified node or -1 if it is not found.
			/// </summary>
			public override sealed int IndexOf(INode node)
			{
				InheritenceReference ir = node as InheritenceReference;
				if(ir != null)
					return _owner._children.IndexOf(ir);
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
				InheritenceReference outNode;
				bool ret = TryGetNode(name, out outNode);
				node = outNode;
				return ret;
			}

			/// <summary>
			/// Attempts to get a node with the specified name.
			/// </summary>
			/// <param name="node">The found node, or null if no node was found.</param>
			/// <returns>Whether a node with the specified name was found.</returns>
			public bool TryGetNode(string name, out InheritenceReference node)
			{
				int i;
				if(int.TryParse(name, out i) && i >= 0 && i < Count)
				{
					node = this[i];
					return true;
				}
				else
				{
					node = null;
					return false;
				}
			}

			/// <summary>
			/// Copies the nodes in this ChildCollection to an array.
			/// </summary>
			/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this InheritenceList.</param>
			/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
			public override sealed void CopyTo(INode[] array, int arrayIndex)
			{
				for(int i = 0; i < Count; i++)
					array[i + arrayIndex] = this[i];
			}

			/// <summary>
			/// Copies the nodes in this ChildCollection to an array.
			/// </summary>
			/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this InheritenceList.</param>
			/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
			public void CopyTo(InheritenceReference[] array, int arrayIndex)
			{
				_owner._children.CopyTo(array);
			}

			/// <summary>
			/// Returns an enumerator that iterates through the nodes in this ChildCollection.
			/// </summary>
			public new IEnumerator<InheritenceReference> GetEnumerator()
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
				if(!(node is InheritenceReference))
					throw new InvalidOperationException("The specified node is not an InheritenceList.");
			}

			/// <summary>
			/// Adds a new node or new nodes defined by the specified text to the end of this ChildCollection.
			/// </summary>
			/// <param name="text">The added node or nodes.</param>
			protected override sealed INode[] AddNodeText(string text)
			{
				return Array.ConvertAll<InheritenceReference, INode>(Add(text), ir => ir);
			}

			/// <summary>
			/// Inserts a new node or new nodes defined by the specified text at the specified index.
			/// </summary>
			/// <returns>The inserted node or nodes.</returns>
			protected override sealed INode[] InsertNodeText(int index, string text)
			{
				return Array.ConvertAll<InheritenceReference, INode>(Insert(index, text), ir => ir);
			}

			/// <summary>
			/// Replaces the specified old node with a new node or nodes defined by the specified text.
			/// </summary>
			/// <returns>The new node or nodes.</returns>
			protected override sealed INode[] ReplaceWithNodeText(INode oldNode, string newText)
			{
				return Array.ConvertAll<InheritenceReference, INode>(Replace(oldNode, newText), ir => ir);
			}

			/// <summary>
			/// Replaces the nodes in the specified range with a new node or nodes defined by the specified text.
			/// </summary>
			/// <returns>The new node or nodes.</returns>
			protected override sealed INode[] ReplaceRangeWithNodeText(int index, int count, string newText)
			{
				return Array.ConvertAll<InheritenceReference, INode>(ReplaceRange(index, count, newText), ir => ir);
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