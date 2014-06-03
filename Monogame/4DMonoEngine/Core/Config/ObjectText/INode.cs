using System.IO;

namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Exposes methods and properties common to all nodes in a data tree.
	/// </summary>
	public interface INode
	{
		#region Properties

		/// <summary>
		/// Gets the parent of this node.
		/// </summary>
		Parent Parent { get; }

		/// <summary>
		/// Gets the ObjectTextFile in which this node resides.
		/// </summary>
		ObjectTextFile FileRoot { get; }

		/// <summary>
		/// Gets the original ObjectTextFile that was deliberately loaded by the application and not automatically loaded to follow a reference.
		/// </summary>
		ObjectTextFile OriginalRoot { get; }

		/// <summary>
		/// Gets the name or string-formatted index of this node.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the index of this node within its parent.
		/// </summary>
		int Index { get; }

		/// <summary>
		/// Gets the depth in FileRoot of this node.
		/// </summary>
		int FileDepth { get; }

		/// <summary>
		/// Gets the full path of this node relative to FileRoot.
		/// </summary>
		string FullPath { get; }

		/// <summary>
		/// Gets whether the name of this node has been hash-tagged and can thus be accessed with "#name" from anywhere.
		/// </summary>
		bool HasHashTag { get; }

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the text that defines this node.
		/// </summary>
		string GetDefiningText();

		/// <summary>
		/// Gets text that defines this node excluding superfluous whitespace and comments.
		/// </summary>
		string GetSignificantText();

		/// <summary>
		/// Writes the text that defines this node to the specified TextWriter.
		/// </summary>
		void WriteTo(TextWriter writer);

		/// <summary>
		/// Formats this node using the specified formatter.
		/// </summary>
		void Format(IFormatter formatter);

		/// <summary>
		/// Finds the node at the specified path relative to this node.
		/// </summary>
		INode FindAtPath(string path);

		/// <summary>
		/// Finds a node at the specified path or creates it if it doesn't exist.
		/// </summary>
		INode MakeAtPath(string path);

		/// <summary>
		/// Attemps to find the node at the specified path relative to this node.
		/// </summary>
		/// <param name="node">The found node or null if it was not found.</param>
		/// <returns>Whether the node was found.</returns>
		bool TryFindAtPath(string path, out INode node);

		/// <summary>
		/// Returns whether there exists a node at the specified path relative to this node.
		/// </summary>
		bool ExistsAtPath(string path);

		#endregion
	}

	/// <summary>
	/// Extends the INode interface with additional internal methods.
	/// </summary>
	internal interface INodeEx : INode
	{
		/// <summary>
		/// Sets the parent of this node.
		/// </summary>
		void SetParent(Parent parent);
	}
}