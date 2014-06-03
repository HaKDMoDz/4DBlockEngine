using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A node which stores an ordered list of InheritenceReferences.
	/// </summary>
	public partial class InheritenceList : Parent, IEnumerable<InheritenceReference>
	{
		#region Private Fields

		private readonly ChildCollection _localChildren;

		private string _insignificant1;
		private List<InheritenceReference> _children = new List<InheritenceReference>();
		private List<string> _insignificants = new List<string>();

		#endregion
		#region Properties

		/// <summary>
		/// Gets the number of nodes in this InheritenceList.
		/// </summary>
		public override sealed int Count
		{
			get { return _localChildren.Count; }
		}

		/// <summary>
		/// Gets the node at the specified string-formatted index.
		/// </summary>
		public new InheritenceReference this[string name]
		{
			get { return _localChildren[name]; }
		}

		/// <summary>
		/// Gets the node at the specified index.
		/// </summary>
		public new InheritenceReference this[int index]
		{
			get { return _localChildren[index]; }
		}

		/// <summary>
		/// Gets the collection of nodes contained in this InheritenceList.
		/// </summary>
		public new ChildCollection LocalChildren
		{
			get { return _localChildren; }
		}

		/// <summary>
		/// Gets all of the string-formatted indices of this InheritenceLists's nodes.
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
		/// Parses a new InheritenceList.
		/// </summary>
		/// <param name="init">The initial token defining this List.</param>
		/// <param name="tok">Tokenizer stream from which tokens are read.</param>
		/// <param name="stop">Rule defining when to stop reading.</param>
		/// <param name="out1">The stop token.</param>
		internal InheritenceList(Token init, Tokenizer tok, StopRule stop, out Token out1)
			: base(null)
		{
			if(init.IsInsignificant())
			{
				_insignificant1 = init.Text;
				Parse(tok.Read(), tok, stop, 0, out out1);
			}
			else
			{
				_insignificant1 = "";
				Parse(init, tok, stop, 0, out out1);
			}

			_localChildren = new ChildCollection(this);
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this InheritenceList.
		/// </summary>
		public override string GetDefiningText()
		{
			// Verify that counts match.
			if(_children.Count != _insignificants.Count)
				throw new Exception("Internal error: Counts of children and insignificants do not match.");

			// Build and return string.
			StringBuilder ret = new StringBuilder();
			ret.Append(_insignificant1);
			for(int i = 0; i < _children.Count; i++)
				ret.Append(_children[i].GetDefiningText() + _insignificants[i]);
			return ret.ToString();
		}

		/// <summary>
		/// Gets text that defines this InheritenceList excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			StringBuilder ret = new StringBuilder();
			for(int i = 0; i < _children.Count; i++)
				ret.Append(_children[i].GetSignificantText());
			return ret.ToString();
		}

		/// <summary>
		/// Gets the text that defines this InheritenceList's body.
		/// </summary>
		public override string GetBodyText()
		{
			return GetDefiningText();
		}

		/// <summary>
		/// Writes the text that defines this InheritenceList to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			// Write each child.
			writer.Write(_insignificant1);
			for(int i = 0; i < _children.Count; i++)
			{
				_children[i].WriteTo(writer);
				writer.Write(_insignificants[i]);
			}
		}

		/// <summary>
		/// Formats this InheritenceList using the specified formatter.
		/// </summary>
		public override void Format(IFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");

			foreach(INode child in _localChildren)
				child.Format(formatter);

			string[] bodyInsignificants = _insignificants.ToArray();
			formatter.Format(this, ref _insignificant1, bodyInsignificants);
			_insignificants = new List<string>(bodyInsignificants);
		}

		/// <summary>
		/// Returns whether this InheritenceList contains the specified node.
		/// </summary>
		public override sealed bool Contains(INode node)
		{
			return _localChildren.Contains(node);
		}

		/// <summary>
		/// Returns whether this InheritenceList contains a node with the specified string-formatted index.
		/// </summary>
		public override sealed bool Contains(string name)
		{
			return _localChildren.Contains(name);
		}

		/// <summary>
		/// Returns whether this InheritenceList contains a InheritenceReference with the specified target.
		/// </summary>
		public bool ContainsReference(string target)
		{
			return _localChildren.ContainsReference(target);
		}

		/// <summary>
		/// Returns the string-formatted index of the specified node or null if it is not found.
		/// </summary>
		public override sealed string NameOf(INode node)
		{
			return _localChildren.NameOf(node);
		}

		/// <summary>
		/// Returns the index of the specified node or -1 if it is not found.
		/// </summary>
		public override sealed int IndexOf(INode node)
		{
			return _localChildren.IndexOf(node);
		}

		/// <summary>
		/// Attempts to get a node with the specified name.
		/// </summary>
		/// <param name="node">The found node, or null if no node was found.</param>
		/// <returns>Whether a node with the specified name was found.</returns>
		public override sealed bool TryGetNode(string name, out INode node)
		{
			return _localChildren.TryGetNode(name, out node);
		}

		/// <summary>
		/// Attempts to get a node with the specified name.
		/// </summary>
		/// <param name="node">The found node, or null if no node was found.</param>
		/// <returns>Whether a node with the specified name was found.</returns>
		public bool TryGetNode(string name, out InheritenceReference node)
		{
			return _localChildren.TryGetNode(name, out node);
		}

		/// <summary>
		/// Copies the nodes in this InheritenceList to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this InheritenceList.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		public override sealed void CopyTo(INode[] array, int arrayIndex)
		{
			_localChildren.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Copies the nodes in this InheritenceList to an array.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the nodes copied from this InheritenceList.</param>
		/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
		public void CopyTo(InheritenceReference[] array, int arrayIndex)
		{
			_localChildren.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Returns an enumerator which iterates through the nodes in this InheritenceList.
		/// </summary>
		public new IEnumerator<InheritenceReference> GetEnumerator()
		{
			return _localChildren.GetEnumerator();
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Returns an enumerator that iterates through the nodes in this InheritenceList.
		/// </summary>
		protected override sealed IEnumerator<INode> GetLocalEnumerator()
		{
			foreach(InheritenceReference ir in this)
				yield return ir;
		}

		/// <summary>
		/// Returns the node with the specified name.
		/// </summary>
		protected override sealed INode GetNodeNamed(string name)
		{
			return this[name];
		}

		/// <summary>
		/// Returns the node at the specified index.
		/// </summary>
		protected override sealed INode GetNodeAt(int index)
		{
			return this[index];
		}

		/// <summary>
		/// Appends the specified text to the initial insignificant body text.
		/// </summary>
		private void AppendBodyInsignificant(string text)
		{
			_insignificant1 += text;
		}

		/// <summary>
		/// Gets the collection of nodes contained in this InheritenceList.
		/// </summary>
		protected override sealed ObjectText.ChildCollection GetLocalChildrenCollection()
		{
			return _localChildren;
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
				// Insignificant?
				if(t.IsInsignificant())
				{
					throw new Exception("Internal error: Unexpected insignificant token.");
				}

				// Stop token?
				else if(stop(t))
				{
					// Set outs and stop reading.
					out1 = t;
					break;
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

					// Reference.
					else
					{
						insert(insertIndex, new InheritenceReference(this, t, tok, stop, out t2, out t3));
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
		/// Inserts a node into the _children list.
		/// </summary>
		private void insert(int index, InheritenceReference node)
		{
			_children.Insert(index, node);
		}

		#endregion
	}
}