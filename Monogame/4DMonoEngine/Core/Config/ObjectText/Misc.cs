using System;
using System.Linq;
using System.Text.RegularExpressions;
using _4DMonoEngine.Core.Config.Parsing;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Returns whether the specified token should cause reading of tokens to stop.
	/// </summary>
	public delegate bool StopRule(Token token);

	/// <summary>
	/// Contains methods to validate ObjectText path strings.
	/// </summary>
	internal static class Validator
	{
		#region Private Static Fields

		private static readonly Regex PATH_RE =
			new Regex(@"^\s*&?\s*(#?[a-zA-Z0-9_-_]+|\^|~|\.|\.\.|<(.*)>|#)?\s*(/\s*([a-zA-Z0-9_-_]+|\^|\.\.|#)\s*)*/?\s*$", RegexOptions.Compiled);

		private static readonly char[] INVALID_FILE_PATH_CHARS = Path.GetInvalidPathChars();

		#endregion
		#region Public Static Methods

		/// <summary>
		/// Validates the syntax of the specified path string.
		/// </summary>
		public static bool ValidatePath(string path)
		{
			if(path == null)
				throw new ArgumentNullException("path");

			/*// This is a hack to circumvent RE bug on Mono.
			if(OSDetector.CurrentOS != OSPlatform.Windows)
				path = path.Replace("..", "DD");*/

			Match m = PATH_RE.Match(path);
			if(m.Success)
			{
				if(path.Contains("<"))
				{
					string filePath = m.Groups[2].Value;
					return filePath.All(c => !INVALID_FILE_PATH_CHARS.Any(c2 => c == c2));
				}
				else
				{
					return true;
				}
			}
			else
			{
				return false;
			}
		}

		#endregion
	}
}