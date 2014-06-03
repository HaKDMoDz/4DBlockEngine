using System;
using System.Collections.Generic;
using System.IO;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Exposes the structure and values of a single ObjectText file.
	/// </summary>
	public class ObjectTextFile : Group
	{
		#region Private Fields

		private readonly ObjectTextFile _root;
		private readonly Dictionary<AbsolutePath, ObjectTextFile> _referencedFiles;
		private readonly Dictionary<string, INode> _hashTags;

		private string insignificant1;
		// <-- group body

		#endregion
		#region Properties

		/// <summary>
		/// Gets the path of the file to which this ObjectTextFile was last loaded or saved.
		/// </summary>
		public AbsolutePath FilePath { get; private set; }

		/// <summary>
		/// Gets this ObjectTextFile or the ObjectTextFile that spawned it.
		/// </summary>
		public override ObjectTextFile OriginalRoot
		{
			get { return _root; }
		}

		/// <summary>
		/// Gets the ObjectTextFiles (including this one) that were loaded or created on-demand by this ObjectTextFile or one of its referenced ObjectTextFiles.
		/// </summary>
		public IEnumerable<ObjectTextFile> ReferencedFiles
		{
			get
			{
				if(_root == this)
				{
					foreach(ObjectTextFile tf in _referencedFiles.Values)
						yield return tf;
				}
				else
				{
					foreach(ObjectTextFile tf in _root._referencedFiles.Values)
						yield return tf;
				}
			}
		}

		/// <summary>
		/// Gets the number of ObjectTextFiles (including this one) that were loaded or created on-demand by this ObjectTextFile or one of its referenced ObjectTextFiles.
		/// </summary>
		public int ReferencedFileCount
		{
			get
			{
				if(_root == this)
					return _referencedFiles.Count;
				else
					return _root._referencedFiles.Count;
			}
		}

		/// <summary>
		/// Gets the hash tags of any hash-tagged nodes in this ObjectTextFile or any referenced ObjectTextFiles.
		/// Hash tags are global to all referenced ObjectTextFiles.
		/// </summary>
		public IEnumerable<string> HashTags
		{
			get
			{
				if(_root != this)
					return _root.HashTags;

				return _hashTags.Keys;
			}
		}

		/// <summary>
		/// Gets the number of hash-tagged nodes in this ObjectTextFile or any referenced ObjectTextFiles.
		/// Hash tags are global to all referenced ObjectTextFiles.
		/// </summary>
		public int HashTagCount
		{
			get
			{
				if(_root != this)
					return _root.HashTagCount;

				return _hashTags.Count;
			}
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Creates a new empty ObjectTextFile.
		/// </summary>
		public ObjectTextFile()
			: base(null)
		{
			insignificant1 = "";
			_root = this;
			FilePath = new AbsolutePath();
			_referencedFiles = new Dictionary<AbsolutePath, ObjectTextFile>();
			_referencedFiles.Add(FilePath, this);
			_hashTags = new Dictionary<string, INode>();
		}

		/// <summary>
		/// Loads a new ObjectTextFile from the file at the specified path.
		/// </summary>
		public ObjectTextFile(string filepath)
			: base(null)
		{
			_root = this;
			FilePath = new AbsolutePath(filepath);
			_referencedFiles = new Dictionary<AbsolutePath, ObjectTextFile>();
			_referencedFiles.Add(FilePath, this);
			_hashTags = new Dictionary<string, INode>();
			Parse();
		}

		/// <summary>
		/// Reads a new ObjectTextFile from the specified StreamReader.
		/// </summary>
		public ObjectTextFile(Stream stream)
			: base(null)
		{
			if(stream == null)
				throw new ArgumentNullException("stream");

			_root = this;
			FilePath = new AbsolutePath();
			_referencedFiles = new Dictionary<AbsolutePath, ObjectTextFile>();
			_referencedFiles.Add(FilePath, this);
			_hashTags = new Dictionary<string, INode>();
			ParseFromStream(stream);
		}

		/// <summary>
		/// Creates a new empty ObjectTextFile and sets its _root to the specified ObjectTextFile.
		/// </summary>
		internal ObjectTextFile(ObjectTextFile root)
			: base(null)
		{
			insignificant1 = "";
			_root = root;
			FilePath = new AbsolutePath();
		}

		/// <summary>
		/// Loads a new ObjectTextFile from the file at the specified path and sets its _root to the specified ObjectTextFile.
		/// </summary>
		internal ObjectTextFile(AbsolutePath path, ObjectTextFile root)
			: base(null)
		{
			_root = root;
			FilePath = path;
			Parse();
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this ObjectTextFile.
		/// </summary>
		public override string GetDefiningText()
		{
			return insignificant1 + base.GetDefiningText();
		}

		/// <summary>
		/// Writes the text that defines this ObjectTextFile to the specified TextWriter.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(insignificant1);
			base.WriteTo(writer);
		}

		/// <summary>
		/// Saves this ObjectTextFile to a file at the specified path.
		/// </summary>
		public void SaveAs(string filepath)
		{
			FilePath = new AbsolutePath(filepath);
			using(StreamWriter writer = new StreamWriter(FilePath))
			{
				WriteTo(writer);
			}
		}

		/// <summary>
		/// Saves this ObjectTextFile and any other ObjectTextFiles that were loaded or created on-demand.
		/// </summary>
		public void SaveAll()
		{
			if(_root == this)
			{
				foreach(ObjectTextFile tf in _referencedFiles.Values)
					tf.Save();
			}
			else
			{
				_root.SaveAll();
			}
		}

		/// <summary>
		/// Saves this ObjectTextFile to the most recently loaded or saved filepath.
		/// </summary>
		public void Save()
		{
			if(FilePath != "")
				SaveAs(FilePath);
			else
				throw new InvalidOperationException("Cannot save the specified ObjectTextFile to the most recently loaded or saved file since it was never loaded from or saved to a file.");
		}

		/// <summary>
		/// Completely empties this ObjectTextFile.
		/// </summary>
		public void Clear()
		{
			insignificant1 = "";
			LocalChildren.Clear();
		}

		/// <summary>
		/// Formats this ObjectTextFile using the specified formatter..
		/// </summary>
		public override void Format(IFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");

			base.Format(formatter);
			formatter.Format(this, ref insignificant1);
		}

		/// <summary>
		/// Returns the node that is registered with the specified hash tag.
		/// Hash tags are global to all referenced ObjectTextFiles.
		/// </summary>
		public INode GetNodeFromHashTag(string hashTag)
		{
			if(_root != this)
				return _root.GetNodeFromHashTag(hashTag);

			INode ret;
			if(TryGetNodeFromHashTag(hashTag, out ret))
				return ret;
			else
				throw new ObjectTextNavigateException("There is no node with hash tag '" + hashTag + "'.");
		}

		/// <summary>
		/// Attempts to get the node that is registered with the specified hash tag.
		/// Hash tags are global to all referenced ObjectTextFiles.
		/// </summary>
		public bool TryGetNodeFromHashTag(string hashTag, out INode node)
		{
			if(_root != this)
				return _root.TryGetNodeFromHashTag(hashTag, out node);

			return _hashTags.TryGetValue(hashTag.ToLowerInvariant(), out node);
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Appends the specified text to the initial insignificant body text.
		/// </summary>
		protected override void AppendBodyInsignificant(string text)
		{
			insignificant1 += text;
		}

		/// <summary>
		/// Returns the InheritenceList associated with this Group, or null if it does not have one.
		/// </summary>
		protected override sealed InheritenceList GetInheritenceList()
		{
			return null;
		}

		/// <summary>
		/// Gets the referenced ObjectTextFile of the specified filename.
		/// Opens the ObjectTextFile if necessary.
		/// </summary>
		internal ObjectTextFile GetReferencedFile(Path file)
		{
			AbsolutePath filepath;
			if(file.IsRooted)
				filepath = new AbsolutePath(file);
			else
				filepath = new AbsolutePath(FilePath.Directory, file);

			if(_root == this)
			{
				// Do we already contain the referenced file?
				ObjectTextFile rf;
				if(_referencedFiles.TryGetValue(filepath, out rf))
					return _referencedFiles[filepath];
				else
				{
					// We don't, so create a new file.
					if(File.Exists(filepath))
					{
						ObjectTextFile tf = new ObjectTextFile(filepath, this);
						_referencedFiles.Add(filepath, tf);
						return tf;
					}
					else
					{
						return null;
					}
				}
			}
			else
			{
				return _root.GetReferencedFile(filepath);
			}
		}

		/// <summary>
		/// Registers the specified hash tag to point to the specified node.
		/// </summary>
		internal void RegisterHashTag(string hashTag, INode node)
		{
			if(_root != this)
			{
				_root.RegisterHashTag(hashTag, node);
				return;
			}

			hashTag = hashTag.ToLowerInvariant();
			if(_hashTags.ContainsKey(hashTag))
				throw new ObjectTextParseException("A different node is already hash-tagged '" + hashTag + "'.");

			_hashTags.Add(hashTag, node);
		}

		/// <summary>
		/// Unregisters the specified hash tag so that it no longer referenced any node.
		/// </summary>
		internal void UnregisterHashTag(string hashTag)
		{
			if(_root != this)
			{
				_root.UnregisterHashTag(hashTag);
				return;
			}

			_hashTags.Remove(hashTag.ToLowerInvariant());
		}

		/// <summary>
		/// Parses this ObjectTextFile from its file path.
		/// </summary>
		private void Parse()
		{
			using(FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
			using(BufferedStream bufStream = new BufferedStream(stream))
			{
				ParseFromStream(bufStream);
			}
		}

		/// <summary>
		/// Parses this ObjectTextFile from the specified StreamReader.
		/// </summary>
		private void ParseFromStream(Stream stream)
		{
			using(BinaryReader reader = new BinaryReader(stream))
			{
				try
				{
					Tokenizer tok = new Tokenizer(reader);
					Token t = tok.Read();
					if(t.IsInsignificant())
					{
						insignificant1 = t.Text;
						t = tok.Read();
					}
					else
					{
						insignificant1 = "";
					}
					Parse(t, tok, StopRule, out t);
				}
				catch(TokenizeException e)
				{
					throw new ObjectTextParseException("Unexpected character (" + e.Input + ") at position " + e.Position + " in file \"" + FilePath + "\".", Token.Empty, FilePath);
				}
				catch(ParseException e)
				{
					throw new ObjectTextParseException("Unable to parse file \"" + FilePath + "\".", e, Token.Empty, FilePath);
				}
			}
		}

		/// <summary>
		/// Returns whether we should stop reading at the specified token.
		/// </summary>
		private static bool StopRule(Token token)
		{
			return token.IsEndOfFile;
		}

		#endregion
	}
}