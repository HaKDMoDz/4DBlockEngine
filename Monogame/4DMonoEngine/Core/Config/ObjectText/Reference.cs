using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A node which stores a symbolic reference to another node.
	/// </summary>
	public abstract class Reference : Node
	{
		#region Events

		/// <summary>
		/// Invoked when the target node of this Reference has changed.
		/// </summary>
		public event EventHandler<EventArgs> TargetChanged;

		#endregion
		#region Private Fields

		private string[] _targetTokens;
		private string _target;

		#endregion
		#region Constructors

		/// <summary>
		/// Initializes a new Reference.
		/// </summary>
		internal Reference(Parent parent)
			: base(parent)
		{
			// Do nothing.
		}

		#endregion
		#region Public Static Methods

		/// <summary>
		/// Replaces the specified old node with a new Reference with the specified target.
		/// </summary>
		public static Reference Replace(INode oldNode, INode target)
		{
			if(target == null)
				throw new ArgumentNullException("target");

			return Replace(oldNode, target.FullPath);
		}

		/// <summary>
		/// Replaces the specified old node with a new Reference with the specified target.
		/// </summary>
		public static Reference Replace(INode oldNode, string target)
		{
			if(oldNode == null)
				throw new ArgumentNullException("oldNode");
			if(target == null)
				throw new ArgumentNullException("target");

			Reference ret;
			if(!(oldNode is Reference))
			{
				if(oldNode is IGroupedNode)
					ret = (Reference)oldNode.Parent.LocalChildren.Replace(oldNode, oldNode.Name + "=&.;")[0];
				else if(oldNode is IListedNode)
					ret = (Reference)oldNode.Parent.LocalChildren.Replace(oldNode, "&.")[0];
				else
					throw new Exception("Internal error: Unable to determine type of oldNode.");
			}
			else
			{
				ret = (Reference)oldNode;
			}
			ret.Target = target;
			return ret;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this Reference.
		/// </summary>
		public override string GetDefiningText()
		{
			StringBuilder ret = new StringBuilder();
			foreach(string t in _targetTokens)
				ret.Append(t);
			return ret.ToString();
		}

		/// <summary>
		/// Gets text that defines this Reference excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			StringBuilder ret = new StringBuilder();
			foreach(string t in _targetTokens)
			{
				if(!t.IsInsignificant())
					ret.Append(t);
			}
			return ret.ToString();
		}

		/// <summary>
		/// Writes the text that defines this Reference to the specified writer.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			foreach(string t in _targetTokens)
				writer.Write(t);
		}

		/// <summary>
		/// Finds the node directly referenced by this Reference.
		/// </summary>
		/// <returns>The found node regardless of whether it itself is a reference.</returns>
		public virtual INode FindNextTarget()
		{
			INode ret;
			if(!Parent.TryFindAtPath("&" + _target, out ret))
				throw new ObjectTextNavigateException("Unable to find next target of Reference at path \"" + FullPath + "\".");
			return ret;
		}

		/// <summary>
		/// Attempts to find the node directly referenced by this Reference.
		/// </summary>
		/// <param name="node">The found node or null if it was not found.</param>
		/// <returns>Whether the node was found.</returns>
		public virtual bool TryFindNextTarget(out INode node)
		{
			return Parent.TryFindAtPath("&" + _target, out node);
		}

		/// <summary>
		/// Returns whether there exists a node at the path specified by this Reference.
		/// </summary>
		public virtual bool NextTargetExists()
		{
			return Parent.ExistsAtPath("&" + _target);
		}

		/// <summary>
		/// Finds the final node referenced by this Reference.
		/// </summary>
		/// <returns>The found node or if itself is a Reference then the node it finally references.</returns>
		public virtual INode FindFinalTarget()
		{
			INode ret;
			if(!Parent.TryFindAtPath(_target, out ret))
				throw new ObjectTextNavigateException("Unable to find final target of Reference at path \"" + FullPath + "\".");
			return ret;
		}

		/// <summary>
		/// Attempts to find the final node referenced by this Reference.
		/// </summary>
		/// <param name="node">The found node or null if it was not found.</param>
		/// <returns>Whether the node was found.</returns>
		public virtual bool TryFindFinalTarget(out INode node)
		{
			return Parent.TryFindAtPath(_target, out node);
		}

		/// <summary>
		/// Returns whether there exists a node at the path specified by this Reference or by any other Reference which this Reference directly or indirectly references.
		/// </summary>
		public virtual bool FinalTargetExists()
		{
			return Parent.ExistsAtPath(_target);
		}

		/// <summary>
		/// Gets or sets the target of this reference.
		/// </summary>
		public string Target
		{
			get { return _target; }
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");

				if(value != _target)
				{
					// Validate target.
					if(!Validator.ValidatePath(_target))
						throw new ArgumentException("The specified target is not a valid path.");

					// Format and parse.
					string formattedTarget = StringTools.FormatString(_target);
					StringStream buf = new StringStream(formattedTarget);
					BinaryReader reader = new BinaryReader(buf);
					Parse(new Tokenizer(reader), token => token.IsEndOfFile);

					InvokeTargetChanged();
				}
			}
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Parses this reference.
		/// </summary>
		private void Parse(Tokenizer tok, StopRule stop)
		{
			Token t1, t2;
			Parse(tok.Read(), tok, stop, null, out t1, out t2);
		}

		/// <summary>
		/// Parses this reference.
		/// </summary>
		/// <param name="init">The initial token defining this reference.</param>
		/// <param name="tok">Tokenizer stream from which tokens are read.</param>
		/// <param name="stop">Rule defining when to stop reading.</param>
		/// <param name="parentStop">Parent's rule defining when to stop reading.</param>
		/// <param name="out1">Insignificant whitespace before stop token.</param>
		/// <param name="out2">The stop token.</param>
		protected void Parse(Token init, Tokenizer tok, StopRule stop, StopRule parentStop, out Token out1, out Token out2)
		{
			List<Token> tokens = new List<Token>();
			Token t;

			// Add initial token.
			tokens.Add(init);

			// Read tokens until we reach the stop token.
			while(true)
			{
				t = tok.Read();
				if(stop(t) || (parentStop != null && parentStop(t)))
					break;
				else if(t.IsEndOfFile)
					throw new ObjectTextParseException("Unexpected end-of-file at " + t.Position + " in file \"" + FileRoot.FilePath + "\".", t, FileRoot.FilePath);
				else
					tokens.Add(t);
			}

			// Set out tokens and remove and insignificant token at end of list.
			if(tokens[tokens.Count - 1].IsInsignificant())
			{
				out1 = tokens[tokens.Count - 1];
				tokens.RemoveAt(tokens.Count - 1);
			}
			else
			{
				out1 = Token.Empty;
			}
			out2 = t;

			// Parse value.
			_targetTokens = tokens.ConvertAll(t2 => t2.Text).ToArray();
			_target = ParseTarget(tokens);
		}

		/// <summary>
		/// Parses a target from the specified tokens.
		/// </summary>
		private string ParseTarget(IList<Token> tokens)
		{
			StringBuilder ret = new StringBuilder();
			foreach(Token t in tokens)
			{
				if(t.IsQuoted())
					ret.Append(StringTools.DeformatString(t.Text));
				else
					ret.Append(t.Text);
			}

			string retStr = ret.ToString();
			if(!Validator.ValidatePath(retStr))
				throw new ObjectTextParseException("The reference target at " + tokens[0].Position + " in file \"" + FileRoot.FilePath + "\" is not a valid path: " + retStr, tokens[0], FileRoot.FilePath);
			return retStr;
		}

		#endregion
		#region Event Invokers

		/// <summary>
		/// Invokes the TargetChanged event.
		/// </summary>
		private void InvokeTargetChanged()
		{
			if(TargetChanged != null)
				TargetChanged(this, EventArgs.Empty);
		}

		#endregion
	}
}