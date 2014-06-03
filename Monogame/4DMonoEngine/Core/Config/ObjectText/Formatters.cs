namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Contains predefined static IFormatter objects.
	/// </summary>
	public static class Formatters
	{
		#region Public Static Fields

		/// <summary>
		/// Formats ObjectText nodes with human-readable spacing, line breaks, and indentation.
		/// </summary>
		public static readonly HumanReadableFormatter HumanReadable = new HumanReadableFormatter();

		/// <summary>
		/// Formats ObjectText nodes with the minimum possible spacing, line breaks, and indentation.
		/// </summary>
		public static readonly NoneFormatter None = new NoneFormatter();

		#endregion
		#region Public Classes

		/// <summary>
		/// Formats ObjectText nodes with human-readable spacing, line breaks, and indentation.
		/// </summary>
		public class HumanReadableFormatter : IFormatter
		{
			public void Format(ObjectTextFile node, ref string insignificant1)
			{
				insignificant1 = "";
			}

			public void Format(ListEx node, ref bool colon, ref string insignificant1)
			{
				string tabs = MakeTabs(node);

				colon = false;
				if(node.Count > 0)
					insignificant1 = "\n\t" + tabs;
				else
					insignificant1 = "";
			}

			public void Format(ListedReference node, ref string insignificant1, ref string insignificant2, ref string terminator)
			{
				insignificant1 = "";
				insignificant2 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(ListedList node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(ListedGroup node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(ListedField node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(ListedDummy node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(List node, string[] bodyInsignificants)
			{
				string tabs = MakeTabs(node);

				for(int i = 0; i < bodyInsignificants.Length - 1; i++)
					bodyInsignificants[i] = "\n" + MakeTabs(node.LocalChildren[i]);
				if(bodyInsignificants.Length > 0)
					bodyInsignificants[bodyInsignificants.Length - 1] = "\n" + tabs;
			}

			public void Format(InheritenceReference node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(InheritenceList node, ref string insignificant1, string[] bodyInsignificants)
			{
				string tabs = MakeTabs(node.Parent);

				if(node.Count > 0)
				{
					insignificant1 = " ";

					for(int i = 0; i < bodyInsignificants.Length - 1; i++)
						bodyInsignificants[i] = " ";

					if(bodyInsignificants.Length > 0)
					{
						if(node.Parent is IGroupedNode)
							bodyInsignificants[bodyInsignificants.Length - 1] = "\n" + tabs;
						else
							bodyInsignificants[bodyInsignificants.Length - 1] = " ";
					}
				}
				else
				{
					if(node.Parent is IGroupedNode)
					{
						if(node.Parent is Group && (node.Parent).Count == 0)
							insignificant1 = "";
						else if(node.Parent is List && (node.Parent).Count == 0)
							insignificant1 = "";
						else
							insignificant1 = "\n" + tabs;
					}
					else
					{
						insignificant1 = "";
					}
				}
			}

			public void Format(GroupEx node, ref bool colon, ref string insignificant1)
			{
				string tabs = MakeTabs(node);

				colon = false;
				if(node.Count > 0)
					insignificant1 = "\n\t" + tabs;
				else
					insignificant1 = "";
			}

			public void Format(GroupedReference node, ref string insignificant1, ref string insignificant2,
							   ref string insignificant3, ref string insignificant4, ref string terminator)
			{
				insignificant1 = " ";
				insignificant2 = " ";
				insignificant3 = "";
				insignificant4 = "";
				terminator = ";";
			}

			public void Format(GroupedList node, ref string insignificant1, ref string equals, ref string insignificant2,
							   ref string insignificant3, ref string terminator)
			{
				insignificant1 = "";
				equals = "";
				insignificant2 = "";
				insignificant3 = "";
				terminator = "";
			}

			public void Format(GroupedGroup node, ref string insignificant1, ref string equals, ref string insignificant2,
							   ref string insignificant3, ref string terminator)
			{
				insignificant1 = "";
				equals = "";
				insignificant2 = "";
				insignificant3 = "";
				terminator = "";
			}

			public void Format(GroupedField node, ref string insignificant1, ref string insignificant2, ref string insignificant3,
							   ref string terminator)
			{
				insignificant1 = " ";
				insignificant2 = " ";
				insignificant3 = "";
				terminator = ";";
			}

			public void Format(GroupedDummy node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = ";";
			}

			public void Format(Group node, string[] bodyInsignificants)
			{
				string tabs = MakeTabs(node);

				for(int i = 0; i < bodyInsignificants.Length - 1; i++)
					bodyInsignificants[i] = "\n" + MakeTabs(node.LocalChildren[i]);
				if(bodyInsignificants.Length > 0)
					bodyInsignificants[bodyInsignificants.Length - 1] = "\n" + tabs;
			}

			/// <summary>
			/// Makes a string full of tabs.
			/// </summary>
			private static string MakeTabs(INode node)
			{
				string tabs = "";
				for(int i = 0; i < node.FileDepth - 1; i++)
					tabs += "\t";

				return tabs;
			}
		}

		/// <summary>
		/// Formats ObjectText nodes with the minimum possible spacing, line breaks, and indentation.
		/// </summary>
		public class NoneFormatter : IFormatter
		{
			public void Format(ObjectTextFile node, ref string insignificant1)
			{
				insignificant1 = "";
			}

			public void Format(ListEx node, ref bool colon, ref string insignificant1)
			{
				colon = false;
				insignificant1 = "";
			}

			public void Format(ListedReference node, ref string insignificant1, ref string insignificant2, ref string terminator)
			{
				insignificant1 = "";
				insignificant2 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(ListedList node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = "";
			}

			public void Format(ListedGroup node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = "";
			}

			public void Format(ListedField node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(ListedDummy node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(List node, string[] bodyInsignificants)
			{
				for(int i = 0; i < bodyInsignificants.Length; i++)
					bodyInsignificants[i] = "";
			}

			public void Format(InheritenceReference node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ",";
			}

			public void Format(InheritenceList node, ref string insignificant1, string[] bodyInsignificants)
			{
				insignificant1 = "";

				for(int i = 0; i < bodyInsignificants.Length; i++)
					bodyInsignificants[i] = "";
			}

			public void Format(GroupEx node, ref bool colon, ref string insignificant1)
			{
				colon = false;
				insignificant1 = "";
			}

			public void Format(GroupedReference node, ref string insignificant1, ref string insignificant2,
							   ref string insignificant3, ref string insignificant4, ref string terminator)
			{
				insignificant1 = "";
				insignificant2 = "";
				insignificant3 = "";
				insignificant4 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ";";
			}

			public void Format(GroupedList node, ref string insignificant1, ref string equals, ref string insignificant2,
							   ref string insignificant3, ref string terminator)
			{
				insignificant1 = "";
				insignificant2 = "";
				insignificant3 = "";
				terminator = "";
			}

			public void Format(GroupedGroup node, ref string insignificant1, ref string equals, ref string insignificant2,
							   ref string insignificant3, ref string terminator)
			{
				insignificant1 = "";
				insignificant2 = "";
				insignificant3 = "";
				terminator = "";
			}

			public void Format(GroupedField node, ref string insignificant1, ref string insignificant2, ref string insignificant3,
							   ref string terminator)
			{
				insignificant1 = "";
				insignificant2 = "";
				insignificant3 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ";";
			}

			public void Format(GroupedDummy node, ref string insignificant1, ref string terminator)
			{
				insignificant1 = "";
				terminator = node.IsLastLocalNodeInParent ? "" : ";";
			}

			public void Format(Group node, string[] bodyInsignificants)
			{
				for(int i = 0; i < bodyInsignificants.Length; i++)
					bodyInsignificants[i] = "";
			}
		}

		#endregion
	}
}