namespace _4DMonoEngine.Core.Config.Parsing
{
	/// <summary>
	/// Identifies a particular action to be performed when a parsing State is entered.
	/// </summary>
	public enum StateAction
	{
		/// <summary>
		/// Removes the peeked character from the input stream and appends it to the end of the output string.
		/// </summary>
		Add,

		/// <summary>
		/// Removes the peeked character from the input stream but does not append it to the output stream.
		/// </summary>
		Skip,

		/// <summary>
		/// Pushes the previously appended character back on to the input stream and removes it from the end of the output string.
		/// </summary>
		Back,
	}
}