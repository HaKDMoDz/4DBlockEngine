namespace _4DMonoEngine.Core.Config.ObjectText
{
	/// <summary>
	/// Exposes methods that format various types of ObjectText nodes.
	/// </summary>
	public interface IFormatter
	{
		void Format(ObjectTextFile node, ref string insignificant1);

		void Format(ListEx node, ref bool colon, ref string insignificant1);

		void Format(ListedReference node, ref string insignificant1, ref string insignificant2, ref string terminator);

		void Format(ListedList node, ref string insignificant1, ref string terminator);

		void Format(ListedGroup node, ref string insignificant1, ref string terminator);

		void Format(ListedField node, ref string insignificant1, ref string terminator);

		void Format(ListedDummy node, ref string insignificant1, ref string terminator);

		void Format(List node, string[] bodyInsignificants);

		void Format(InheritenceReference node, ref string insignificant1, ref string terminator);

		void Format(InheritenceList node, ref string insignificant1, string[] bodyInsignificants);

		void Format(GroupEx node, ref bool colon, ref string insignificant1);

		void Format(GroupedReference node, ref string insignificant1, ref string insignificant2, ref string insignificant3, ref string insignificant4, ref string terminator);

		void Format(GroupedList node, ref string insignificant1, ref string equals, ref string insignificant2, ref string insignificant3, ref string terminator);

		void Format(GroupedGroup node, ref string insignificant1, ref string equals, ref string insignificant2, ref string insignificant3, ref string terminator);

		void Format(GroupedField node, ref string insignificant1, ref string insignificant2, ref string insignificant3, ref string terminator);

		void Format(GroupedDummy node, ref string insignificant1, ref string terminator);

		void Format(Group node, string[] bodyInsignificants);
	}
}