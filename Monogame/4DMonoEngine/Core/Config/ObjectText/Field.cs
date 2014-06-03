using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// A node which stores a single text value.
	/// </summary>
	public abstract class Field : Node
	{
		#region Events

		/// <summary>
		/// Invoked when the value text of this Field has changed.
		/// </summary>
		public event EventHandler<EventArgs> ValueChanged;

		#endregion
		#region Private Fields

		private string[] _valueTokens;
		private string _value;

		#endregion
		#region Constructors

		/// <summary>
		/// Initializes a new Field.
		/// </summary>
		internal Field(Parent parent)
			: base(parent)
		{
			// Do nothing.
		}

		#endregion
		#region Public Static Methods

		/// <summary>
		/// Replaces the specified old node with a new Field with the specified value.
		/// </summary>
		public static Field Replace(INode oldNode, string value)
		{
			if(oldNode == null)
				throw new ArgumentNullException("oldNode");
			if(value == null)
				throw new ArgumentNullException("value");

			Field ret;
			if(!(oldNode is Field))
			{
				if(oldNode is IGroupedNode)
					ret = (Field)oldNode.Parent.LocalChildren.Replace(oldNode, oldNode.Name + "=\"\";")[0];
				else if(oldNode is IListedNode)
					ret = (Field)oldNode.Parent.LocalChildren.Replace(oldNode, "\"\"")[0];
				else
					throw new Exception("Internal error: Unable to determine type of oldNode.");
			}
			else
			{
				ret = (Field)oldNode;
			}
			ret.Value = value;
			return ret;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this node.
		/// </summary>
		public override string GetDefiningText()
		{
			StringBuilder ret = new StringBuilder();
			foreach(string t in _valueTokens)
				ret.Append(t);
			return ret.ToString();
		}

		/// <summary>
		/// Gets text that defines this node excluding superfluous whitespace and comments.
		/// </summary>
		public override string GetSignificantText()
		{
			StringBuilder ret = new StringBuilder();
			foreach(string t in _valueTokens)
			{
				if(t.IsInsignificant())
					ret.Append(" ");
				else
					ret.Append(t);
			}
			return ret.ToString();
		}

		/// <summary>
		/// Writes the text that defines this node to the specified writer.
		/// </summary>
		public override void WriteTo(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			foreach(string t in _valueTokens)
				writer.Write(t);
		}

		/// <summary>
		/// Gets or sets the text value of this Field.
		/// </summary>
		public string Value
		{
			get { return _value; }
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");

				if(value != _value)
				{
					// Force format?
					bool force;
					if(value.Length == 0 ||
					   value.Contains("&") ||
					   value.Contains(";") ||
					   value.Contains(",") ||
					   value.Contains("{") ||
					   value.Contains("}") ||
					   value.Contains("[") ||
					   value.Contains("]") ||
					   value.Contains("?"))
					{
						force = true;
					}
					else
					{
						force = false;
					}

					// Format and parse.
					string formattedValue = StringTools.FormatString(value, force);
					StringStream buf = new StringStream(formattedValue);
					BinaryReader reader = new BinaryReader(buf);
					Parse(new Tokenizer(reader), token => token.IsEndOfFile);

					// Sanity check.
					if(value != _value)
						throw new Exception("Internal error: Values mismatch.");

					InvokeValueChanged();
				}
			}
		}

		#endregion
		#region Non-Public Methods

		/// <summary>
		/// Parses this Field.
		/// </summary>
		private void Parse(Tokenizer tok, StopRule stop)
		{
			Token t1, t2;
			Parse(tok.Read(), tok, stop, null, out t1, out t2);
		}

		/// <summary>
		/// Parses this Field.
		/// </summary>
		/// <param name="init">The initial token defining this Field.</param>
		/// <param name="tok">Tokenizer stream from which tokens are read.</param>
		/// <param name="stop">Rule defining when to stop reading.</param>
		/// <param name="parentStop">Parent's rule defining when to stop reading.</param>
		/// <param name="out1">Insignificant text before stop token.</param>
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
					throw new ObjectTextParseException("Unexpected end-of-file at position " + t.Position + " in file \"" + FileRoot.FileDepth + "\".", t, FileRoot.FilePath);
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
			_valueTokens = tokens.ConvertAll(t2 => t2.Text).ToArray();
			_value = ParseValue(_valueTokens);
		}

		#endregion
		#region Event Invokers

		/// <summary>
		/// Invokes the ValueChanged event.
		/// </summary>
		private void InvokeValueChanged()
		{
			if(ValueChanged != null)
				ValueChanged(this, EventArgs.Empty);
		}

		#endregion
		#region Private Static Methods

		/// <summary>
		/// Parses a value from the specified tokens.
		/// </summary>
		private static string ParseValue(IEnumerable<string> tokens)
		{
			StringBuilder ret = new StringBuilder();
			foreach(string t in tokens)
			{
				if(t.IsInsignificant())
					ret.Append(" ");
				else if(t.IsQuoted())
					ret.Append(StringTools.DeformatString(t));
				else
					ret.Append(t);
			}
			return ret.ToString();
		}

		#endregion
	}
}