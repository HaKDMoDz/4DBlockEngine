using System;
using System.Linq;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Contains various extension methods for dealing with tokens.
	/// </summary>
	internal static class Extensions
	{
		#region Extensions

		/// <summary>
		/// Returns whether the specified token is significant
		/// </summary>
		public static bool IsInsignificant(this Token token)
		{
			return token.Text.IsInsignificant();
		}

		/// <summary>
		/// Returns whether the specified token is significant
		/// </summary>
		public static bool IsInsignificant(this string token)
		{
			if(token == null)
				throw new ArgumentNullException("token");

			if(token.Length > 0)
			{
				return
					token[0] == ' ' || token[0] == '\n' || token[0] == '\r' || token[0] == '\t' ||
					token.StartsWith("/*") || token.StartsWith("//");
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// Returns whether the specified token is quoted.
		/// </summary>
		public static bool IsQuoted(this Token token)
		{
			return token.Text.IsQuoted();
		}

		/// <summary>
		/// Returns whether the specified token is quoted.
		/// </summary>
		public static bool IsQuoted(this string token)
		{
			if(token == null)
				throw new ArgumentNullException("token");

			if(token.Length > 0)
				return (token.StartsWith("\"") || token.StartsWith("@\"")) && token.EndsWith("\"");
			else
				return false;
		}

		/// <summary>
		/// Returns whether the specified token is a valid identifier.
		/// </summary>
		public static bool IsIdentifier(this Token token)
		{
			return token.Text.IsIdentifier();
		}

		/// <summary>
		/// Returns whether the specified token is a valid identifier.
		/// </summary>
		public static bool IsIdentifier(this string token)
		{
			if(token == null)
				throw new ArgumentNullException("token");

			if(token.Length > 0)
			{
				if(char.IsDigit(token[0]))
				{
					return false;
				}
				else if(token[0] == '#')
				{
					if(token.Length < 2)
						return false;
					for(int i = 1; i < token.Length; i++)
					{
						char c = token[i];
						if(!char.IsLetterOrDigit(c) && c != '_')
							return false;
					}
					return true;
				}
				else
				{
					return token.All(c => char.IsLetterOrDigit(c) || c == '_');
				}
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Returns whether the specified token is hash-tagged.
		/// </summary>
		public static bool IsHashTagged(this Token token)
		{
			return token.Text.IsHashTagged();
		}

		/// <summary>
		/// Returns whether the specified token is hash-tagged.
		/// </summary>
		public static bool IsHashTagged(this string token)
		{
			if(token == null)
				throw new ArgumentNullException("token");

			return token.Length > 0 && token[0] == '#';
		}

		/// <summary>
		/// If the specified token starts with a # symbol, returns the result of removing it.
		/// </summary>
		public static string Unhashed(this string token)
		{
			if(token.IsHashTagged())
				return token.Substring(1);
			else
				return token;
		}

		/// <summary>
		/// Returns whether the specified token is a valid index.
		/// </summary>
		public static bool IsIndex(this Token token)
		{
			return token.Text.IsIndex();
		}

		/// <summary>
		/// Returns whether the specified token is a valid index.
		/// </summary>
		public static bool IsIndex(this string token)
		{
			if(token == null)
				throw new ArgumentNullException("token");

			return token.All(char.IsDigit);
		}

		#endregion
	}
}