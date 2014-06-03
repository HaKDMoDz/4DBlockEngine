using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A node which stores an ordered list of child nodes which are also uniquely accessible by name.
	/// </summary>
	public abstract partial class Group : Parent, IEnumerable<IGroupedNode>
	{
		#region Private Fields

		private readonly ChildCollection _localChildrenCollection;
		private readonly List<IGroupedNode> _children = new List<IGroupedNode>();
		private readonly Dictionary<string, IGroupedNode> _childrenByName = new Dictionary<string, IGroupedNode>();
		private List<string> _insignificants = new List<string>();

		#endregion
		#region Properties

		/// <summary>
		/// Gets the number of nodes in this Group.
		/// </summary>
		public override sealed int Count
		{
			get { return GetCountIncludingInherited(new Stack<Group>()); }
		}

		/// <summary>
		/// Gets the node with the specified name.
		/// </summary>
		public new IGroupedNode this[string name]
		{
			get
			{
				IGroupedNode ret;
				if(TryGetNode(name, out ret))
					return ret;
				else
					throw new KeyNotFoundException("The specified Group does not contain a node by the specified name.");
			}
		}

		/// <summary>
		/// Gets the node at the specified index.
		/// </summary>
		public new IGroupedNode this[int index]
		{
			get
			{
				if(index < 0)
					throw new ArgumentOutOfRangeException("index");
				IGroupedNode ret = GetNodeAtIncludingInherited(ref index, new Stack<Group>());
				if(ret == null)
					throw new ArgumentOutOfRangeException("index");
				return ret;
			}
		}

		/// <summary>
		/// Gets the collection of nodes contained locally by this Group.
		/// </summary>
		public new ChildCollection LocalChildren
		{
			get { return _localChildrenCollection; }
		}

		/// <summary>
		/// Gets all of the names used by nodes including inherited nodes in this Group.
		/// The returned names will not necessarily be unique if nodes in a child Group have the same names as nodes in an inherited Group.
		/// </summary>
		public override IEnumerable<string> Names
		{
			get
			{
				foreach(IGroupedNode gn in this)
					yield return gn.Name;
			}
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Initializes a new Group.
		/// </summary>
		internal Group(Parent parent)
			: base(parent)
		{
			_localChildrenCollection = new ChildCollection(this);
		}

		#endregion
		#region Public Static Methods

		/// <summary>
		/// Replaces the specified old node with a new Group that contains the specified nodes and inherits from the specified Groups.
		/// </summary>
		public static GroupEx Replace(INode oldNode, string[] childNames, Group[] inheritedGroups)
		{
			string[] targets = null;
			if(inheritedGroups != null)
			{
				targets = new string[inheritedGroups.Length];
				for(int i = 0; i < targets.Length; i++)
					targets[i] = inheritedGroups[i].FullPath;
			}

			return Replace(oldNode, childNames, targets);
		}

		/// <summary>
		/// Replaces the specified old node with a new Group that contains the specified nodes and inherits from the specified Groups.
		/// </summary>
		public static GroupEx Replace(INode oldNode, string[] childNames, string[] inheritedGroups)
		{
			if(oldNode == null)
				throw new ArgumentNullException("oldNode");

			GroupEx ret;
			if(!(oldNode is GroupEx))
			{
				// Replace.
				if(oldNode is IGroupedNode)
					ret = (GroupEx)oldNode.Parent.LocalChildren.Replace(oldNode, oldNode.Name + "{}")[0];
				else if(oldNode is IListedNode)
					ret = (GroupEx)oldNode.Parent.LocalChildren.Replace(oldNode, "{}")[0];
				else
					throw new Exception("Internal error: Unable to determine type of oldNode.");

				// Add nodes.
				if(childNames != null)
				{
					foreach(string name in childNames)
						ret.LocalChildren.Add(name + ";");
				}

				// Add inherited groups.
				if(inheritedGroups != null)
				{
					foreach(string target in inheritedGroups)
						ret.InheritenceList.LocalChildren.Add(target);
				}
			}
			else
			{
				ret = (GroupEx)oldNode;

				// Set child nodes.
				if(childNames != null)
				{
					// Remove any nodes that should not exist.
					foreach(IGroupedNode node in ret.LocalChildren)
					{
						if(Array.IndexOf(childNames, node.Name) == -1)
							ret.LocalChildren.Remove(node);
					}

					// Add any nodes that should exist.
					foreach(string name in childNames)
					{
						if(!ret.LocalChildren.Contains(name))
							ret.LocalChildren.Add(name + ";");
					}
				}
				else
				{
					// Clear all nodes since it shouldn't have any children.
					ret.LocalChildren.Clear();
				}

				// Set inherited groups.
				if(inheritedGroups != null)
				{
					// Remove any inherited groups that should not exist.
					foreach(InheritenceReference ir in ret.InheritenceList.LocalChildren)
					{
						if(Array.IndexOf(inheritedGroups, ir.Target) == -1)
							ret.InheritenceList.LocalChildren.Remove(ir);
					}

					// Add any inherited groups that should exist.
					foreach(string target in inheritedGroups)
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
		/// Gets the text that defines this Group.
		/// </summary>
		public override string GetDefiningText()
		{
			// Build and return string.
			StringBuilder ret = new StringBuilder();
			for(int i = 0; i < _children.Count; i++)
				ret.Append(_children[i].GetDefiningText() + _insignificants[i]);
			return ret.ToString();
		}

		/// <summary>
		/// Gets text that defines this Group excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			StringBuilder ret = new StringBuilder();
			for(int i = 0; i < _children.Count; i++)
				ret.Append(_children[i].GetSignificantText());
			return ret.ToString();
		}

		/// <summary>
		/// Gets the text that defines this Group's body.
		/// </summary>
		public override string GetBodyText()
		{
			return GetDefiningText();
		}

		/// <summary>
		/// Writes the text that defines this Group to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			// Write each child.
			for(int i = 0; i < _children.Count; i++)
			{
				_children[i].WriteTo(writer);
				writer.Write(_insignificants[i]);
			}
		}

		/// <summary>
		/// Formats this Group using the specified formatter.
		/// </summary>
		public override void Format(IFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");

			foreach(IGroupedNode child in _localChildrenCollection)
				child.Format(formatter);

			string[] bodyInsignificants = _insignificants.ToArray();
			formatter.Format(this, bodyInsignificants);
			_insignificants = new List<string>(bodyInsignificants);
		}

		/// <summary>
		/// Returns whether this Group contains the specified node including inherited nodes.
		/// </summary>
		public override sealed bool Contains(INode node)
		{
			return ContainsIncludingInherited(node, new Stack<Group>());
		}

		/// <summary>
		/// Returns whether this Group contains a node including inherited nodes with the specified name.
		/// </summary>
		public override sealed bool Contains(string name)
		{
			return ContainsIncludingInherited(name, new Stack<Group>());
		}

		/// <summary>
		/// Returns the name of the specified node including inherited nodes or null if this Group does not contain the specified node.
		/// </summary>
		public override sealed string NameOf(INode node)
		{
			return NameOfIncludingInherited(node, new Stack<Group>());
		}

		/// <summary>
		/// Returns the index of the specified node including inherited nodes or -1 if it is not found.
		/// </summary>
		public override sealed int IndexOf(INode node)
		{
			return IndexOfIncludingInherited(node, new Stack<Group>());
		}

		/// <summary>
		/// Attempts to get a node including inherited nodes with the specified name.
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
		/// Attempts to get a node including inherited nodes with the specified name.
		/// </summary>
		/// <param name="node">The found node, or null if no node was found.</param>
		/// <returns>Whether a node with the specified name was found.</returns>
		public bool TryGetNode(string name, out IGroupedNode node)
		{
			return TryGetNodeIncludingInherited(name, out node, new Stack<Group>());
		}

		/// <summary>
		/// Copies the nodes including inherited nodes in this Group to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this List.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		public override sealed void CopyTo(INode[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException("array");

			CopyToIncludingInherited(array, ref arrayIndex, new Stack<Group>());
		}

		/// <summary>
		/// Copies the nodes in this Group to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this List.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		public void CopyTo(IGroupedNode[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException("array");

			CopyToIncludingInherited(array, ref arrayIndex, new Stack<Group>());
		}

		/// <summary>
		/// Gets an enumerator which iterates through the nodes in this Group.
		/// </summary>
		public new IEnumerator<IGroupedNode> GetEnumerator()
		{
			foreach(IGroupedNode gn in GetEnumerableIncludingInherited(new Stack<Group>()))
				yield return gn;
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Returns whether this Group contains the specified node including inherited nodes.
		/// </summary>
		private bool ContainsIncludingInherited(INode node, Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Check for containership.
			bool ret = false;
			if(_localChildrenCollection.Contains(node))
			{
				ret = true;
			}
			else
			{
				foreach(Group g in GetInheritedGroups())
				{
					if(g.ContainsIncludingInherited(node, visited))
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
		/// Returns whether this Group contains a node with the specified name including inherited nodes.
		/// </summary>
		private bool ContainsIncludingInherited(string name, Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Check for containership.
			bool ret = false;
			if(_localChildrenCollection.Contains(name))
			{
				ret = true;
			}
			else
			{
				foreach(Group g in GetInheritedGroups())
				{
					if(g.ContainsIncludingInherited(name, visited))
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
		/// Returns the string-formatted index of the specified node including inherited nodes or null if it is not found.
		/// </summary>
		private string NameOfIncludingInherited(INode node, Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Get name.
			string ret = _localChildrenCollection.NameOf(node);
			if(ret == null)
			{
				foreach(Group g in GetInheritedGroups())
				{
					ret = g.NameOfIncludingInherited(node, visited);
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
		private int IndexOfIncludingInherited(INode node, Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Get index.
			int ret = _localChildrenCollection.IndexOf(node);
			if(ret == -1)
			{
				foreach(Group g in GetInheritedGroups())
				{
					ret = g.IndexOfIncludingInherited(node, visited);
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
		private bool TryGetNodeIncludingInherited(string name, out IGroupedNode node, Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Try to get node.
			bool ret = _localChildrenCollection.TryGetNode(name, out node);
			if(ret == false)
			{
				foreach(Group g in GetInheritedGroups())
				{
					ret = g.TryGetNodeIncludingInherited(name, out node, visited);
					if(ret)
						break;
				}
			}

			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Copies the nodes in this Group to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this Group.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		private void CopyToIncludingInherited(INode[] array, ref int arrayIndex, Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Copy.
			foreach(Group g in GetInheritedGroups())
				g.CopyToIncludingInherited(array, ref arrayIndex, visited);
			_localChildrenCollection.CopyTo(array, arrayIndex);
			arrayIndex += _localChildrenCollection.Count;

			visited.Pop();
		}

		/// <summary>
		/// Copies the nodes in this Group including inherited nodes to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this Group.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		private void CopyToIncludingInherited(IGroupedNode[] array, ref int arrayIndex, Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Copy.
			foreach(Group g in GetInheritedGroups())
				g.CopyToIncludingInherited(array, ref arrayIndex, visited);
			_localChildrenCollection.CopyTo(array, arrayIndex);
			arrayIndex += _localChildrenCollection.Count;

			visited.Pop();
		}

		/// <summary>
		/// Returns an enumerable which iterates through the nodes including inherited nodes in this Group.
		/// </summary>
		private IEnumerable<IGroupedNode> GetEnumerableIncludingInherited(Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Yield each node.
			foreach(Group g in GetInheritedGroups())
			{
				foreach(IGroupedNode gn in g.GetEnumerableIncludingInherited(visited))
					yield return gn;
			}
			foreach(IGroupedNode gn in _localChildrenCollection)
				yield return gn;

			visited.Pop();
		}

		/// <summary>
		/// Returns the number of nodes in this Group including inherited nodes.
		/// </summary>
		private int GetCountIncludingInherited(Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Get count.
			int ret = _localChildrenCollection.Count;
			foreach(Group g in GetInheritedGroups())
				ret += g.GetCountIncludingInherited(visited);

			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Returns the node at the specified index including inherited nodes.
		/// </summary>
		private IGroupedNode GetNodeAtIncludingInherited(ref int index, Stack<Group> visited)
		{
			// Check for self-inheritence.
			if(visited.Contains(this))
				throw NewSelfInheritenceException();
			visited.Push(this);

			// Get node.
			IGroupedNode ret = null;
			foreach(Group g in GetInheritedGroups())
			{
				ret = g.GetNodeAtIncludingInherited(ref index, visited);
				if(ret != null)
					break;
			}
			if(ret == null && index < _localChildrenCollection.Count)
			{
				ret = _localChildrenCollection[index];
				index -= _localChildrenCollection.Count;
			}

			visited.Pop();
			return ret;
		}

		/// <summary>
		/// Returns an enumerator which iterates through only the local nodes in this Group.
		/// </summary>
		protected override sealed IEnumerator<INode> GetLocalEnumerator()
		{
			return _children.GetEnumerator();
		}

		/// <summary>
		/// Returns the node including inherited nodes with the specified name.
		/// </summary>
		protected override sealed INode GetNodeNamed(string name)
		{
			return this[name];
		}

		/// <summary>
		/// Returns the node including inherited nodes at the specified index.
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
		/// Returns the collection of nodes contained locally by this Group.
		/// </summary>
		protected override sealed ObjectText.ChildCollection GetLocalChildrenCollection()
		{
			return _localChildrenCollection;
		}

		/// <summary>
		/// Returns the InheritenceList associated with this Group, or null if it does not have one.
		/// </summary>
		protected abstract InheritenceList GetInheritenceList();

		/// <summary>
		/// Returns an enumerable class which iterates through the Groups inherited by this Group, if any.
		/// </summary>
		private IEnumerable<Group> GetInheritedGroups()
		{
			InheritenceList il = GetInheritenceList();
			if(il != null)
			{
				foreach(InheritenceReference ir in il)
				{
					INode node = ir.FindFinalTarget();
					if(node is Group)
						yield return ((Group)node);
					else
						throw new ObjectTextNavigateException("The Group at path \"" + FullPath + "\" specifies that it inherits from a Group at path \"" + node.FullPath + "\" but the node at that path is not a Group.");
				}
			}
		}

		/// <summary>
		/// Returns a new exception explaining that this Group inherits from itself.
		/// </summary>
		private Exception NewSelfInheritenceException()
		{
			return new InvalidOperationException("The Group at path \"" + FullPath + "\" inherits from itself.");
		}

		/// <summary>
		/// Changes the internal record of the name of the specified node.
		/// </summary>
		internal void RenameChild(string oldName, string newName)
		{
			oldName = oldName.ToLower();
			newName = newName.ToLower();

			if(_childrenByName.ContainsKey(newName))
				throw new InvalidOperationException("The specified group already contains a node with the specified name.");

			IGroupedNode node = _childrenByName[oldName];
			_childrenByName.Remove(oldName);
			_childrenByName.Add(newName, node);
		}

		/// <summary>
		/// Parses this Group.
		/// </summary>
		/// <param name="init">The initial token defining this Group.</param>
		/// <param name="tok">Tokenizer stream from which tokens are read.</param>
		/// <param name="stop">Token at which to stop reading.</param>
		/// <param name="out1">The stop token.</param>
		protected void Parse(Token init, Tokenizer tok, StopRule stop, out Token out1)
		{
			Parse(init, tok, stop, _localChildrenCollection.Count, out out1);
		}

		/// <summary>
		/// Parses this Group.
		/// </summary>
		/// <param name="init">The initial token defining this Group.</param>
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
					Token? t2; // Insignificant text before this group's stop token, or null if no stop token was read.
					Token? t3; // This group's stop token, or null if none was read.

					// Identifier.
					if(t.IsIdentifier())
					{
						Token _name = t;
						Token _insignificant1;
						Token _equals;
						Token _insignificant2;
						Token _init;

						// Parse tokens.
						t = tok.Read();
						if(t.IsInsignificant())
						{
							_insignificant1 = t;
							t = tok.Read();
						}
						else
						{
							_insignificant1 = Token.Empty;
						}
						if(t.Text == "=")
						{
							_equals = t;
							t = tok.Read();
							if(t.IsInsignificant())
							{
								_insignificant2 = t;
								t = tok.Read();
								_init = t;
							}
							else
							{
								_insignificant2 = Token.Empty;
								_init = t;
							}
						}
						else
						{
							_equals = Token.Empty;
							_insignificant2 = Token.Empty;
							_init = t;
						}

						// Dummy with terminator?
						if(_equals.Text == "" && (_init.Text == ";" || _init.Text == ","))
						{
							Insert(insertIndex, new GroupedDummy(this, _name, _insignificant1, _init));
							t2 = null;
							t3 = null;
						}

						// List.
						else if(_init.Text == "[")
						{
							InheritenceList inheritenceList = new InheritenceList(_init, tok, token => token.Text == "[", out _init);
							Insert(insertIndex, new GroupedList(this, _name, _insignificant1, _equals, _insignificant2, Token.Empty,
								inheritenceList, _init, tok, out t2, out t3));
						}

						// Group.
						else if(_init.Text == "{")
						{
							InheritenceList inheritenceList = new InheritenceList(_init, tok, token => token.Text == "{", out _init);
							Insert(insertIndex, new GroupedGroup(this, _name, _insignificant1, _equals, _insignificant2, Token.Empty,
								inheritenceList, _init, tok, out t2, out t3));
						}

						// Group or list with inheritence list.
						else if(_init.Text == ":")
						{
							// Parse inheritence list.
							Token temp;
							InheritenceList inheritenceList = new InheritenceList(
								tok.Read(),
								tok,
								token => token.Text == "{" || token.Text == "[",
								out temp);

							// List?
							if(temp.Text == "[")
							{
								Insert(insertIndex, new GroupedList(this, _name, _insignificant1, _equals, _insignificant2, _init, inheritenceList,
									temp, tok, out t2, out t3));
							}

							// Group?
							else if(temp.Text == "{")
							{
								Insert(insertIndex, new GroupedGroup(this, _name, _insignificant1, _equals, _insignificant2, _init, inheritenceList,
									temp, tok, out t2, out t3));
							}

							// Parse error.
							else
							{
								throw new ObjectTextParseException(temp, FileRoot.FilePath);
							}
						}

						// Dummy without semicolon?
						else if(_equals.Text == "" && stop(_init))
						{
							Insert(insertIndex, new GroupedDummy(this, _name, Token.Empty, Token.Empty));
							t2 = _insignificant1;
							t3 = _init;
						}

						// Reference.
						else if(_equals.Text == "=" && _init.Text == "&")
						{
							Insert(insertIndex, new GroupedReference(this, _name, _insignificant1, _equals, _insignificant2, _init, tok, stop, out t2, out t3));
						}

						// Field.
						else if(_equals.Text == "=")
						{
							Insert(insertIndex, new GroupedField(this, _name, _insignificant1, _equals, _insignificant2, _init, tok, stop, out t2, out t3));
						}

						// Something unexpected.
						else
						{
							throw new ObjectTextParseException(_init, FileRoot.FilePath);
						}
					}

					// Something unexpected.
					else
					{
						throw new ObjectTextParseException(t, FileRoot.FilePath);
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

					// Increment insert index.
					insertIndex++;
				}
			}

			// Verify that counts match.
			if(_children.Count != _insignificants.Count)
				throw new Exception("Internal error: Counts of children and insignificants do not match.");
		}

		/// <summary>
		/// Adds a node to the child list and the child dictionary.
		/// </summary>
		private void Insert(int index, IGroupedNodeEx node)
		{
			string name = node.Name.ToLower();

			// Make sure we don't already have a node of the same name.
			if(_childrenByName.ContainsKey(name))
				throw new ObjectTextParseException("Group at path \"" + FullPath + "\" already contains a node named \"" + node.Name + "\".", Token.Empty, FileRoot.FilePath);

			// Make sure previous node has a semicolon after it if it needs one.
			if(index == _children.Count && index > 0)
			{
				IGroupedNodeEx n = (IGroupedNodeEx)_children[index - 1];
				if(!n.HasTerminator && (n is GroupedDummy || n is GroupedField || n is GroupedReference))
					n.HasTerminator = true;
			}
			else if(index < _children.Count && !node.HasTerminator
			        && (node is GroupedDummy || node is GroupedField || node is GroupedReference))
			{
				node.HasTerminator = true;
			}

			_children.Insert(index, node);
			_childrenByName.Add(name, node);
		}

		#endregion
	}
}