using System;
using System.Collections.Generic;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;
using PTokenizer  = _4DMonoEngine.Core.Config.Parsing.Tokenizer;
using PTokenizerFSM  = _4DMonoEngine.Core.Config.Parsing.TokenizerFSM;
using PToken = _4DMonoEngine.Core.Config.Parsing.Token;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// The base class for all nodes.
	/// </summary>
	public abstract class Node : INodeEx
	{
		#region Private Static Fields

		private static readonly PTokenizerFSM _initFsm;
		private static readonly PTokenizerFSM _normFsm;

		#endregion
		#region Static Constructors

		/// <summary>
		/// Static Constructor.
		/// </summary>
		static Node()
		{
			Predicate<int> isSlash = input => input == '/';
			Predicate<int> isAmpersand = input => input == '&';
			Predicate<int> isTilde = input => input == '~';
			Predicate<int> isIdentifier = input => (input >= 'a' && input <= 'z') || (input >= 'A' && input <= 'Z') || (input >= '0' && input <= '9') || input == '_' || input == '#';
			Predicate<int> isDot = input => input == '.';
			Predicate<int> isCaret = input => input == '^';
			Predicate<int> isPound = input => input == '#';
			Predicate<int> isWhitespace = input => input == ' ' || input == '\n' || input == '\r' || input == '\t';
			Predicate<int> isOpenBracket = input => input == '<';
			Predicate<int> isCloseBracket = input => input == '>';

			_initFsm = new PTokenizerFSM(
				new State(StateAction.Skip, Transition.Error, Transition.Return,
					new Transition(isWhitespace, 0),
					new Transition(isOpenBracket, 1),
					new Transition(isDot, 4),
					new Transition(isIdentifier, 6),
					new Transition(isTilde, 7),
					new Transition(isAmpersand, 7),
					new Transition(isSlash, 7),
					new Transition(isCaret, 7)),
				new State(StateAction.Add, 2, Transition.Error,
					new Transition(isCloseBracket, 3)),
				new State(StateAction.Add, 2, Transition.Error,
					new Transition(isCloseBracket, 3)),
				new State(StateAction.Add, Transition.Return, Transition.Return),
				new State(StateAction.Add, Transition.Return, Transition.Return,
					new Transition(isDot, 5)),
				new State(StateAction.Add, Transition.Return, Transition.Return),
				new State(StateAction.Add, Transition.Return, Transition.Return,
					new Transition(isIdentifier, 6)),
				new State(StateAction.Add, Transition.Return, Transition.Return));

			_normFsm = new PTokenizerFSM(
				new State(StateAction.Skip, Transition.Error, Transition.Return,
					new Transition(isWhitespace, 0),
					new Transition(isSlash, 1)),
				new State(StateAction.Skip, Transition.Error, Transition.Return,
					new Transition(isWhitespace, 2),
					new Transition(isIdentifier, 3),
					new Transition(isCaret, 4),
					new Transition(isPound, 4),
					new Transition(isDot, 5)),
				new State(StateAction.Skip, Transition.Error, Transition.Return,
					new Transition(isWhitespace, 2),
					new Transition(isIdentifier, 3),
					new Transition(isCaret, 4),
					new Transition(isPound, 4)),
				new State(StateAction.Add, Transition.Return, Transition.Return,
					new Transition(isIdentifier, 3)),
				new State(StateAction.Add, Transition.Return, Transition.Return),
				new State(StateAction.Add, Transition.Error, Transition.Error,
					new Transition(isDot, 4)));
		}

		#endregion
		#region Private Fields

		private Parent _parent;

		#endregion
		#region Properties

		/// <summary>
		/// Gets the parent of this node.
		/// </summary>
		public Parent Parent
		{
			get { return _parent; }
		}

		/// <summary>
		/// Gets the ObjectTextFile in which this node resides.
		/// </summary>
		public ObjectTextFile FileRoot
		{
			get
			{
				Node n = this;
				while(n.Parent != null)
					n = n.Parent;
				return (ObjectTextFile)n;
			}
		}

		/// <summary>
		/// Gets the original ObjectTextFile that was deliberately loaded by the application and not automatically loaded to follow a reference.
		/// </summary>
		public virtual ObjectTextFile OriginalRoot
		{
			get { return FileRoot.OriginalRoot; }
		}

		/// <summary>
		/// Gets the name or string-formatted index of this node.
		/// </summary>
		public string Name
		{
			get
			{
				if(_parent != null)
					return _parent.NameOf(this);
				else
					throw new InvalidOperationException("The specified Node does not have a name since it is the root of an ObjectText file.");
			}
		}

		/// <summary>
		/// Gets the index of this node within its parent.
		/// </summary>
		public int Index
		{
			get
			{
				if(_parent != null)
					return _parent.IndexOf(this);
				else
					throw new InvalidOperationException("The specified Node does not have an index since it is the root of an ObjectText file.");
			}
		}

		/// <summary>
		/// Gets the index of this node within its parent.
		/// </summary>
		public bool IsLastLocalNodeInParent
		{
			get
			{
				if(_parent != null)
					return _parent.LocalChildren.IndexOf(this) == _parent.LocalChildren.Count - 1;
				else
					throw new InvalidOperationException("The specified Node does not have a parent since it is the root of an ObjectText file.");
			}
		}

		/// <summary>
		/// Gets the depth in FileRoot of this node.
		/// </summary>
		public int FileDepth
		{
			get
			{
				Node n = this;
				int i = 0;
				while(n._parent != null)
				{
					n = n._parent;
					i++;
				}
				return i;
			}
		}

		/// <summary>
		/// Gets the full path of this node relative to FileRoot.
		/// </summary>
		public string FullPath
		{
			get
			{
				if(_parent != null)
					return _parent.FullPath + "/" + _parent.NameOf(this);
				else
					return "<" + FileRoot.FilePath + ">";
			}
		}

		/// <summary>
		/// Gets whether the name of this node has been hash-tagged and can thus be accessed with "#name" from anywhere.
		/// </summary>
		public bool HasHashTag
		{
			get { return GetHashTag() != null; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Initializes a new Node.
		/// </summary>
		internal Node(Parent parent)
		{
			_parent = parent;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this node.
		/// </summary>
		public override sealed string ToString()
		{
			return FullPath;
		}

		/// <summary>
		/// Gets the text that defines this node.
		/// </summary>
		public abstract string GetDefiningText();

		/// <summary>
		/// Gets text that defines this node excluding superfluous whitespace and comments.
		/// </summary>
		public abstract string GetSignificantText();

		/// <summary>
		/// Writes the text that defines this node to the specified writer.
		/// </summary>
		public abstract void WriteTo(TextWriter writer);

		/// <summary>
		/// Formats this node using the specified formatter.
		/// </summary>
		public abstract void Format(IFormatter formatter);

		/// <summary>
		/// Finds the node at the specified path relative to this node.
		/// </summary>
		public INode FindAtPath(string path)
		{
			INode ret = FindAtPath(path, false, new List<INode>());
			if(ret == null)
				throw new ObjectTextNavigateException("Unable to find node at path \"" + path + "\".");
			return ret;
		}

		/// <summary>
		/// Finds a node at the specified path or creates it if it doesn't exist.
		/// </summary>
		public INode MakeAtPath(string path)
		{
			INode ret = FindAtPath(path, true, new List<INode>());
			if(ret == null)
				throw new ObjectTextNavigateException("Unable to find or make node at path \"" + path + "\".");
			return ret;
		}

		/// <summary>
		/// Finds a node at the specified path relative to the specified node or creates it if it doesn't exist.
		/// </summary>
		public static INode MakeAtPath(ref INode node, string path)
		{
			if(node == null)
				throw new ArgumentNullException("node");

			Parent parent = node.Parent;
			string name = parent != null ? node.Name : null;
			INode ret = node.MakeAtPath(path);
			if(parent != null && node.Parent == null)
				node = parent[name];
			return ret;
		}

		/// <summary>
		/// Attemps to find the node at the specified path relative to this Node.
		/// </summary>
		/// <param name="node">The found node or null if it was not found.</param>
		/// <returns>Whether the node was found.</returns>
		public bool TryFindAtPath(string path, out INode node)
		{
			node = FindAtPath(path, false, new List<INode>());
			return node != null;
		}

		/// <summary>
		/// Returns whether there exists a node at the specified path relative to this Node.
		/// </summary>
		public bool ExistsAtPath(string path)
		{
			return FindAtPath(path, false, new List<INode>()) != null;
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Sets the parent of this node.
		/// </summary>
		void INodeEx.SetParent(Parent parent)
		{
			SetParent(parent);
		}

		/// <summary>
		/// Sets the parent of this node.
		/// </summary>
		internal virtual void SetParent(Parent parent)
		{
			if(parent != _parent)
			{
				UnregisterHashTag();
				_parent = parent;
				RegisterHashTag();
			}
		}

		/// <summary>
		/// Registers a hash tag for this node if the HasHashTag property is true.
		/// </summary>
		protected void RegisterHashTag()
		{
			string hashTag = GetHashTag();
			if(hashTag != null)
			{
				ObjectTextFile root = FileRoot;
				if(root != null)
					root.RegisterHashTag(hashTag, this);
			}
		}

		/// <summary>
		/// Unregisters the hash tag for this node if the HasHashTag property is true.
		/// </summary>
		protected void UnregisterHashTag()
		{
			string hashTag = GetHashTag();
			if(hashTag != null)
			{
				ObjectTextFile root = FileRoot;
				if(root != null)
					root.UnregisterHashTag(hashTag);
			}
		}

		/// <summary>
		/// Returns the hash tag for this node, or null if it is not hash-tagged.
		/// </summary>
		protected virtual string GetHashTag()
		{
			return null;
		}

		/// <summary>
		/// Finds the node at the specified path relative to this Node.
		/// </summary>
		/// <param name="makePath">Whether a node should be created at the specified path if none was found.</param>
		/// <param name="traversed">The list of nodes traversed while finding the final node.</param>
		private INode FindAtPath(string path, bool makePath, ICollection<INode> traversed)
		{
			if(path == null)
				throw new ArgumentNullException("path");
			if(traversed == null)
				throw new ArgumentNullException("traversed");

			INode cur;

			// Blank string means this node.
			if(path == "")
				return this;

			// Validate path syntax.
			if(!Validator.ValidatePath(path))
				throw new ArgumentException("Invalid syntax for path \"" + path + "\".", "path");

			// Create initial tokenizer.
			StringStream buf = new StringStream(path);
			BinaryReader reader = new BinaryReader(buf);
			PTokenizer initTok = new PTokenizer(reader, _initFsm);

			// Read initial token.
			Token? t = initTok.Read();

			// Suppress dereferencing?
			bool deref;
			if(t.Value.Text == "&")
			{
				deref = false;
				t = initTok.Read();
			}
			else
			{
				deref = true;
			}

			// File reference?
			if(t.Value.Text.StartsWith("<") && t.Value.Text.EndsWith(">"))
			{
				string filepath = t.Value.Text.Substring(1, t.Value.Text.Length - 2);
				cur = FileRoot.GetReferencedFile((Path)filepath);
				if(cur == null)
				{
					if(makePath)
						cur = new ObjectTextFile(FileRoot);
					else
						return null;
				}
				t = null;
			}

			// Original root?
			else if(t.Value.Text == "/")
			{
				cur = OriginalRoot;
				t = initTok.Read();
			}

			// File root?
			else if(t.Value.Text == "~")
			{
				cur = FileRoot;
				t = null;
			}

			// This node?
			else if(t.Value.Text == ".")
			{
				cur = this;
				t = null;
			}

			// Hash tagged?
			else if(t.Value.IsHashTagged())
			{
				cur = FileRoot.GetNodeFromHashTag(t.Value.Text);
				t = null;
			}

			// No root specified, so assume this node.
			else
			{
				cur = this;
			}

			// Create normal tokenizer.
			PTokenizer normTok = new PTokenizer(reader, initTok.Position, _normFsm);

			// Read first normal token.
			if(t == null)
			{
				t = normTok.Read();
			}

			// Keep looping until we have parsed the entire path.
			while(t.Value.Text != "")
			{
				// If the current node is a reference then immediately dereference it.
				if(cur is Reference)
				{
					Reference r = (Reference)cur;
					if(!r.TryFindFinalTarget(out cur))
						return null;
				}

				// Get next cur.
				if(t.Value.Text == "^")
				{
					if(cur is ListEx)
						cur = ((ListEx)cur).InheritenceList;
					else if(cur is GroupEx)
						cur = ((GroupEx)cur).InheritenceList;
					else
						return null;
				}
				else if(t.Value.Text == "..")
				{
					if(cur.Parent != null)
						cur = cur.Parent;
					else
						return null;
				}
				else if(cur is Parent)
				{
					Parent p = (Parent)cur;
					if(!p.TryGetNode(t.Value.Text, out cur))
					{
						if(makePath)
						{
							if(p is List && t.Value.Text == "#")
								cur = p.LocalChildren.Add("?")[0];
							else if(p is List && t.Value.IsIndex() && int.Parse(t.Value.Text) == 0)
								cur = p.LocalChildren.Add("?")[0];
							else if(p is Group && t.Value.IsIdentifier())
								cur = p.LocalChildren.Add(t.Value.Text + ";")[0];
							else
								return null;
						}
						else
						{
							return null;
						}
					}
				}
				else if(makePath && cur is Dummy)
				{
					if(t.Value.Text == "#" || (t.Value.IsIndex() && int.Parse(t.Value.Text) == 0))
						cur = List.Replace(cur, 1, (string[])null)[0];
					else if(t.Value.IsIdentifier())
						cur = Group.Replace(cur, new[] {t.Value.Text}, (string[])null)[t.Value.Text];
					else
						return null;
				}
				else
				{
					return null;
				}

				// Read next token.
				t = normTok.Read();
			}

			// Dereference the found node?
			if(cur is Reference && deref)
			{
				Reference r = (Reference)cur;

				// Check for circular references.
				if(traversed.Contains(cur))
					return null;
				else
					traversed.Add(cur);

				// Try to find a node at the specified path.
				cur = r._parent.FindAtPath(r.Target, false, traversed);
				if(cur == null)
					return null;
			}

			return cur;
		}

		#endregion
	}
}