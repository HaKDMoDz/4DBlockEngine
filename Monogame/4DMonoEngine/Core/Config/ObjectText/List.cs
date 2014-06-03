using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A node which stores an ordered list of child nodes.
	/// </summary>
	public abstract partial class List : Parent, IEnumerable<IListedNode>
	{
		#region Private Fields

		private readonly ChildCollection _localChildren;
		private readonly List<IListedNode> _children = new List<IListedNode>();
		private List<string> _insignificants = new List<string>();

		#endregion
		#region Properties

		/// <summary>
		/// Gets the number of nodes in this List including inherited nodes.
		/// </summary>
		public override sealed int Count
		{
			get { return GetCoundIncludingInherited(new Stack<List>()); }
		}

		/// <summary>
		/// Gets the node including inherited nodes at the specified string-formatted index.
		/// </summary>
		public new IListedNode this[string name]
		{
			get
			{
				IListedNode ret;
				if(TryGetNode(name, out ret))
					return ret;
				else
					throw new KeyNotFoundException("The specified List does not contain a node by the specified name.");
			}
		}

		/// <summary>
		/// Gets the node including inherited nodes at the specified index.
		/// </summary>
		public new IListedNode this[int index]
		{
			get
			{
				if(index < 0)
					throw new ArgumentOutOfRangeException("index");
				IListedNode ret = GetNodeAtEx(index, new Stack<List>());
				if(ret == null)
					throw new ArgumentOutOfRangeException("index");
				return ret;
			}
		}

		/// <summary>
		/// Gets the collection of nodes contained locally in this List.
		/// </summary>
		public new ChildCollection LocalChildren
		{
			get { return _localChildren; }
		}

		/// <summary>
		/// Gets all of the string-formatted indices of nodes including inherited nodes in this Parent.
		/// </summary>
		public override IEnumerable<string> Names
		{
			get
			{
				for(int i = 0; i < Count; i++)
					yield return i.ToString(CultureInfo.InvariantCulture);
			}
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Initializes a new List.
		/// </summary>
		internal List(Parent parent)
			: base(parent)
		{
			_localChildren = new ChildCollection(this);
		}

		#endregion
		#region Public Static Methods

		/// <summary>
		/// Replaces the specified old node with a new List that contains the specified nodes and inherits from the specified Lists.
		/// </summary>
		public static ListEx Replace(INode oldNode, int childCount, List[] inheritedLists)
		{
			string[] targets = null;
			if(inheritedLists != null)
			{
				targets = new string[inheritedLists.Length];
				for(int i = 0; i < targets.Length; i++)
					targets[i] = inheritedLists[i].FullPath;
			}

			return Replace(oldNode, childCount, targets);
		}

		/// <summary>
		/// Replaces the specified node with a new List that contains the specified number of children and inherits from the specified Lists.
		/// </summary>
		public static ListEx Replace(INode oldNode, int childCount, string[] inheritedLists)
		{
			if(oldNode == null)
				throw new ArgumentNullException("oldNode");

			ListEx ret;
			if(!(oldNode is ListEx))
			{
				// Replace.
				if(oldNode is IGroupedNode)
					ret = (ListEx)oldNode.Parent.LocalChildren.Replace(oldNode, oldNode.Name + "[]")[0];
				else if(oldNode is IListedNode)
					ret = (ListEx)oldNode.Parent.LocalChildren.Replace(oldNode, "[]")[0];
				else
					throw new Exception("Internal error: Unable to determine type of oldNode.");

				// Add nodes.
				for(int i = 0; i < childCount; i++)
					ret.LocalChildren.Add("?");

				// Add inherited lists.
				if(inheritedLists != null)
				{
					foreach(string target in inheritedLists)
						ret.InheritenceList.LocalChildren.Add(target);
				}
			}
			else
			{
				ret = (ListEx)oldNode;

				// Add/remove children.
				if(childCount < ret.LocalChildren.Count)
				{
					do
					{
						ret.LocalChildren.RemoveAt(ret.LocalChildren.Count - 1);
					}
					while(childCount < ret.LocalChildren.Count);
				}
				else if(childCount > ret.LocalChildren.Count)
				{
					do
					{
						ret.LocalChildren.Add("?");
					}
					while(childCount > ret.LocalChildren.Count);
				}

				// Set inherited groups.
				if(inheritedLists != null)
				{
					// Remove any inherited groups that should not exist.
					foreach(InheritenceReference ir in ret.InheritenceList.LocalChildren)
					{
						if(Array.IndexOf(inheritedLists, ir.Target) == -1)
							ret.InheritenceList.LocalChildren.Remove(ir);
					}

					// Add any inherited groups that should exist.
					foreach(string target in inheritedLists)
					{
						if(!ret.InheritenceList.LocalChildren.ContainsReference(target))
							ret.InheritenceList.LocalChildren.Add(target);
					}
				}
				else
				{
					// Clear all inherited groups since it shouldn't have any.
					ret.InheritenceList.LocalChildren.Clear();
				}
			}
			return ret;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this List.
		/// </summary>
		public override string GetDefiningText()
		{
			StringBuilder ret = new StringBuilder();
			for(int i = 0; i < _children.Count; i++)
				ret.Append(_children[i].GetDefiningText() + _insignificants[i]);
			return ret.ToString();
		}

		/// <summary>
		/// Gets text that defines this List excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			StringBuilder ret = new StringBuilder();
			for(int i = 0; i < _children.Count; i++)
				ret.Append(_children[i].GetSignificantText());
			return ret.ToString();
		}

		/// <summary>
		/// Gets the text that defines this List's body.
		/// </summary>
		public override string GetBodyText()
		{
			return GetDefiningText();
		}

		/// <summary>
		/// Writes the text that defines this List to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			for(int i = 0; i < _children.Count; i++)
			{
				_children[i].WriteTo(writer);
				writer.Write(_insignificants[i]);
			}
		}

		/// <summary>
		/// Formats this List using the specified formatter.
		/// </summary>
		public override void Format(IFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");

			foreach(IListedNode child in _localChildren)
				child.Format(formatter);

			string[] bodyInsignificants = _insignificants.ToArray();
			formatter.Format(this, bodyInsignificants);
			_insignificants = new List<string>(bodyInsignificants);
		}

		/// <summary>
		/// Returns whether this List contains the specified node including inherited nodes.
		/// </summary>
		public override sealed bool Contains(INode node)
		{
			return ContainsIncludingInherited(node, new Stack<List>());
		}

		/// <summary>
		/// Returns whether this List contains a node with the specified name including inherited nodes.
		/// </summary>
		public override sealed bool Contains(string name)
		{
			return ContainsIncludingInherited(name, new Stack<List>());
		}

		/// <summary>
		/// Returns the name or string-formatted index of the specified node including inherited nodes or null if it is not found.
		/// </summary>
		public override sealed string NameOf(INode node)
		{
			return NameOfIncludingInherited(node, new Stack<List>());
		}

		/// <summary>
		/// Returns the index of the specified node including inherited nodes or -1 if it is not found.
		/// </summary>
		public override sealed int IndexOf(INode node)
		{
			return IndexOfIncludingInherited(node, new Stack<List>());
		}

		/// <summary>
		/// Attempts to get a node with the specified name including inherited nodes.
		/// </summary>
		/// <param name="node">The found node, or null if no node was found.</param>
		/// <returns>Whether a node with the specified name was found.</returns>
		public override sealed bool TryGetNode(string name, out INode node)
		{
			IListedNode outNode;
			bool ret = TryGetNode(name, out outNode);
			node = outNode;
			return ret;
		}

		/// <summary>
		/// Attempts to get a node with the specified name including inherited nodes.
		/// </summary>
		/// <param name="node">The found node, or null if no node was found.</param>
		/// <returns>Whether a node with the specified name was found.</returns>
		public bool TryGetNode(string name, out IListedNode node)
		{
			return TryGetNodeIncludingInherited(name, out node, new Stack<List>());
		}

		/// <summary>
		/// Copies the nodes in this List including inherited nodes to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this List.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		public override sealed void CopyTo(INode[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException("array");

			CopyToIncludingInherited(array, ref arrayIndex, new Stack<List>());
		}

		/// <summary>
		/// Copies the nodes in this List to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this List.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		public void CopyTo(IListedNode[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException("array");

			CopyToIncludingInherited(array, ref arrayIndex, new Stack<List>());
		}

		/// <summary>
		/// Gets an enumerator which iterates through the nodes in this List including inherited nodes.
		/// </summary>
		public new IEnumerator<IListedNode> GetEnumerator()
		{
			foreach(IListedNode ln in GetEnumerableIncludingInherited(new Stack<List>()))
				yield return ln;
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Returns whether this List contains the specified node including inherited nodes.
		/// </summary>
		private bool ContainsIncludingInherited(INode node, Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Check for containership.
			bool ret = false;
			if(_localChildren.Contains(node))
			{
				ret = true;
			}
			else
			{
				foreach(List l in GetInheritedLists())
				{
					if(l.ContainsIncludingInherited(node, visited))
					{
						ret = true;
						break;
					}
				}
			}

			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Returns whether this List including inherited nodess contains a node with the specified string-formatted index.
		/// </summary>
		private bool ContainsIncludingInherited(string name, Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Check for containership.
			bool ret = false;
			if(_localChildren.Contains(name))
			{
				ret = true;
			}
			else
			{
				foreach(List l in GetInheritedLists())
				{
					if(l.ContainsIncludingInherited(name, visited))
					{
						ret = true;
						break;
					}
				}
			}

			// Return.
			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Returns the string-formatted index including inherited nodes of the specified node or null if it is not found.
		/// </summary>
		private string NameOfIncludingInherited(INode node, Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Get name.
			string ret = _localChildren.NameOf(node);
			if(ret == null)
			{
				foreach(List l in GetInheritedLists())
				{
					ret = l.NameOfIncludingInherited(node, visited);
					if(ret != null)
						break;
				}
			}

			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Returns the index of the specified node including inherited nodes or -1 if it is not found.
		/// </summary>
		private int IndexOfIncludingInherited(INode node, Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Get index.
			int ret = _localChildren.IndexOf(node);
			if(ret == -1)
			{
				foreach(List l in GetInheritedLists())
				{
					ret = l.IndexOfIncludingInherited(node, visited);
					if(ret != -1)
						break;
				}
			}

			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Attempts to get a node including inherited nodes with the specified name.
		/// </summary>
		/// <param name="node">The found node, or null if no node was found.</param>
		/// <returns>Whether a node with the specified name was found.</returns>
		private bool TryGetNodeIncludingInherited(string name, out IListedNode node, Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Try to get node.
			bool ret = _localChildren.TryGetNode(name, out node);
			if(ret == false)
			{
				foreach(List l in GetInheritedLists())
				{
					ret = l.TryGetNodeIncludingInherited(name, out node, visited);
					if(ret)
						break;
				}
			}

			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Copies the nodes in this List including inherited nodes to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this List.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		private void CopyToIncludingInherited(INode[] array, ref int arrayIndex, Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Copy.
			foreach(List l in GetInheritedLists())
				l.CopyToIncludingInherited(array, ref arrayIndex, visited);
			_localChildren.CopyTo(array, arrayIndex);
			arrayIndex += _localChildren.Count;

			visited.Pop();
		}

		/// <summary>
		/// Copies the nodes in this List to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this List.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		private void CopyToIncludingInherited(IListedNode[] array, ref int arrayIndex, Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Copy.
			foreach(List l in GetInheritedLists())
				l.CopyToIncludingInherited(array, ref arrayIndex, visited);
			_localChildren.CopyTo(array, arrayIndex);
			arrayIndex += _localChildren.Count;

			// Return.
			visited.Pop();
		}

		/// <summary>
		/// Gets an enumerable which iterates through the nodes including inherited nodes in this List.
		/// </summary>
		private IEnumerable<IListedNode> GetEnumerableIncludingInherited(Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Yield each node.
			foreach(List l in GetInheritedLists())
			{
				foreach(IListedNode ln in l.GetEnumerableIncludingInherited(visited))
					yield return ln;
			}
			foreach(IListedNode ln in _localChildren)
				yield return ln;

			visited.Pop();
		}

		/// <summary>
		/// Gets the number of nodes in this List including inherited nodes.
		/// </summary>
		private int GetCoundIncludingInherited(Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Get count.
			int ret = _localChildren.Count;
			foreach(List l in GetInheritedLists())
				ret += l.GetCoundIncludingInherited(visited);

			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Gets the node at the specified index including inherited nodes.
		/// </summary>
		private IListedNode GetNodeAtEx(int index, Stack<List> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Get node.
			IListedNode ret = null;
			foreach(List l in GetInheritedLists())
			{
				ret = l.GetNodeAtEx(index, visited);
				if(ret != null)
					break;
				index -= l.Count;
			}
			if(ret == null && index < _localChildren.Count)
			{
				ret = _localChildren[index];
			}

			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the nodes in this List.
		/// </summary>
		protected override sealed IEnumerator<INode> GetLocalEnumerator()
		{
			foreach(IListedNode ln in this)
				yield return ln;
		}

		/// <summary>
		/// Returns the node with the specified name including inherited nodes.
		/// </summary>
		protected override sealed INode GetNodeNamed(string name)
		{
			return this[name];
		}

		/// <summary>
		/// Returns the node at the specified index including inherited nodes.
		/// </summary>
		protected override sealed INode GetNodeAt(int index)
		{
			return this[index];
		}

		/// <summary>
		/// Appends the specified text to the initial insignificant body text.
		/// </summary>
		protected abstract void AppendBodyInsignificant(string text);

		/// <summary>
		/// Returns the collection of nodes contained locally in this List.
		/// </summary>
		protected override sealed ObjectText.ChildCollection GetLocalChildrenCollection()
		{
			return _localChildren;
		}

		/// <summary>
		/// Returns the InheritenceList associated with this List, or null if it does not have one.
		/// </summary>
		protected abstract InheritenceList GetInheritenceList();

		/// <summary>
		/// Returns an enumerable class which iterates through the Lists inherited by this List, if any.
		/// </summary>
		private IEnumerable<List> GetInheritedLists()
		{
			InheritenceList il = GetInheritenceList();
			if(il != null)
			{
				foreach(InheritenceReference ir in il)
				{
					INode node = ir.FindFinalTarget();
					if(node is List)
						yield return ((List)node);
					else
						throw new ObjectTextNavigateException("The List at path \"" + FullPath + "\" specifies that it inherits from a List at path \"" + node.FullPath + "\" but the node at that path is not a List.");
				}
			}
		}

		/// <summary>
		/// Returns a new exception explaining that this List inherits from itself.
		/// </summary>
		private Exception NewSelfInheritenceException()
		{
			return new InvalidOperationException("The List at path \"" + FullPath + "\" inherits from itself.");
		}

		/// <summary>
		/// Parses this List.
		/// </summary>
		/// <param name="init">The initial token defining this List.</param>
		/// <param name="tok">Tokenizer stream from which tokens are read.</param>
		/// <param name="stop">Rule defining when to stop reading.</param>
		/// <param name="out1">The stop token.</param>
		protected void Parse(Token init, Tokenizer tok, StopRule stop, out Token out1)
		{
			Parse(init, tok, stop, _localChildren.Count, out out1);
		}

		/// <summary>
		/// Parses this List.
		/// </summary>
		/// <param name="init">The initial token defining this List.</param>
		/// <param name="tok">Tokenizer stream from which tokens are read.</param>
		/// <param name="stop">Rule defining when to stop reading.</param>
		/// <param name="insertIndex">Index at which new nodes will be inserted.</param>
		/// <param name="out1">The stop token.</param>
		private void Parse(Token init, Tokenizer tok, StopRule stop, int insertIndex, out Token out1)
		{
			Token t = init;

			// Keep looping until we hit the stop token.
			while(true)
			{
				// Stop token?
				if(stop(t))
				{
					// Set outs and stop reading.
					out1 = t;
					break;
				}

				// Insignificant?
				else if(t.IsInsignificant())
				{
					throw new Exception("Internal error: Unexpected insignificant token.");
				}
				else
				{
					Token? t2; // Insignificant text before this list's stop token, or null if no stop token was read.
					Token? t3; // This list's stop token, or null if none was read.

					// Comma or semicolon?
					if(t.Text == "," || t.Text == ";")
					{
						throw new ObjectTextParseException(t, FileRoot.FilePath);
					}

					// Question mark?
					else if(t.Text == "?")
					{
						Insert(insertIndex, new ListedDummy(this, t, tok, stop, out t2, out t3));
					}

					// Ampersand?
					else if(t.Text == "&")
					{
						Insert(insertIndex, new ListedReference(this, t, tok, stop, out t2, out t3));
					}

					// Begin bracket?
					else if(t.Text == "[")
					{
						InheritenceList inheritenceList = new InheritenceList(t, tok, token => token.Text == "[", out t);
						Insert(insertIndex, new ListedList(this, Token.Empty, inheritenceList, t, tok, out t2, out t3));
					}

					// Begin brace?
					else if(t.Text == "{")
					{
						InheritenceList inheritenceList = new InheritenceList(t, tok, token => token.Text == "{", out t);
						Insert(insertIndex, new ListedGroup(this, Token.Empty, inheritenceList, t, tok, out t2, out t3));
					}

					// Colon?
					else if(t.Text == ":")
					{
						// Parse inheritence list.
						Token temp;
						InheritenceList inheritenceList = new InheritenceList( tok.Read(), tok, token => token.Text == "{" || token.Text == "[", out temp);

						// Begin bracket?
						if(temp.Text == "[")
						{
							Insert(insertIndex, new ListedList(this, t, inheritenceList, temp, tok, out t2, out t3));
						}

						// Begin brace?
						else if(temp.Text == "{")
						{
							Insert(insertIndex, new ListedGroup(this, t, inheritenceList, temp, tok, out t2, out t3));
						}

						// Parse error.
						else
						{
							throw new ObjectTextParseException(temp, FileRoot.FilePath);
						}
					}

					// Value.
					else
					{
						Insert(insertIndex, new ListedField(this, t, tok, stop, out t2, out t3));
					}

					// Did the new node read any extra tokens?
					if(t2 != null && t3 != null)
					{
						// Add insignificant and set next token.
						_insignificants.Insert(insertIndex, t2.Value.Text);
						t = t3.Value;
					}

					// Did the new node NOT read any extra tokens?
					else if(t2 == null && t3 == null)
					{
						// Add insignificant and read the next token.
						t = tok.Read();
						if(t.IsInsignificant())
						{
							_insignificants.Insert(insertIndex, t.Text);
							t = tok.Read();
						}
						else
						{
							_insignificants.Insert(insertIndex, "");
						}
					}

					// Something bad happened!
					else
					{
						throw new Exception("Internal error: Tokens outputted from new node inconsistent.");
					}

					// Increment Insert index.
					insertIndex++;
				}
			}

			// Verify that counts match.
			if(_children.Count != _insignificants.Count)
				throw new Exception("Internal error: Counts of children and insignificants do not match.");
		}

		/// <summary>
		/// Inserts a node into this List.
		/// </summary>
		private void Insert(int index, IListedNodeEx node)
		{
			// Make sure previous node has a comma after it if it needs one.
			if(index == _children.Count && index > 0)
			{
				IListedNodeEx n = (IListedNodeEx)_children[index - 1];
				if(!n.HasTerminator && (n is ListedDummy || n is ListedField || n is ListedReference))
					n.HasTerminator = true;
			}
			else if(index < _children.Count && !node.HasTerminator
			        && (node is ListedDummy || node is ListedField || node is ListedReference))
			{
				node.HasTerminator = true;
			}

			_children.Insert(index, node);
		}

		#endregion
	}
}