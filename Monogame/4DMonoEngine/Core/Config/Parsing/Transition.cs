using System;

namespace _4DMonoEngine.Core.Config.Parsing
{
	/// <summary>
	/// Defines the a condition and target state of one possible transition between states.
	/// </summary>
	public struct Transition
	{
		#region Public Constants

		/// <summary>
		/// When specified as a transition's target, causes the Tokenizer to return the current string.
		/// </summary>
		public const int Return = -1;

		/// <summary>
		/// When specified as a transition's target, causes the Tokenizer to throw a TokenizeException.
		/// </summary>
		public const int Error = -2;

		#endregion
		#region Private Fields

		private readonly Predicate<int> _condition;
		private readonly int _target;

		#endregion
		#region Constructors

		/// <summary>
		/// Initializes a new state Transition.
		/// </summary>
		/// <param name="condition">The condition predicate that determines whether this transition will be followed.</param>
		/// <param name="target">The target State to move to if the rule returns true, or -1 if the completed token should be returned.</param>
		public Transition(Predicate<int> condition, int target)
		{
			if(condition == null)
				throw new ArgumentNullException("condition");

			_condition = condition;
			_target = target;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the condition predicate that determines whether this transition will be followed.
		/// </summary>
		public Predicate<int> Condition
		{
			get { return _condition; }
		}

		/// <summary>
		/// Gets the target State to move to if the rule returns true, or -1 if the completed token should be returned.
		/// </summary>
		public int Target
		{
			get { return _target; }
		}

		#endregion
	}
}