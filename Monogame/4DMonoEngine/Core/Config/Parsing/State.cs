using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace _4DMonoEngine.Core.Config.Parsing
{
	/// <summary>
	/// Represents a single state in a TokenizerFSM.
	/// </summary>
	public class State
	{
		#region Private Fields

		private readonly StateAction _action;
		private readonly int _defaultTransition;
		private readonly int _eofTransition;
		private readonly Transition[] _transitions;
		private readonly ReadOnlyCollection<Transition> _readOnlyTransitions;
		private readonly Dictionary<int, int> _transitionCache = new Dictionary<int, int>();

		#endregion
		#region Properties

		/// <summary>
		/// Gets the action to perform when this state is entered.
		/// </summary>
		public StateAction Action
		{
			get { return _action; }
		}

		/// <summary>
		/// Gets the default state to move to if none of the transition rules return true.
		/// </summary>
		public int DefaultTransition
		{
			get { return _defaultTransition; }
		}

		/// <summary>
		/// Gets the state to move to when the end of the stream is reached.
		/// </summary>
		public int EOFTransition
		{
			get { return _eofTransition; }
		}

		/// <summary>
		/// Gets the target states that may be moved to from this state along with the conditions that determine when to move to those states.
		/// </summary>
		public ReadOnlyCollection<Transition> Transitions
		{
			get { return _readOnlyTransitions; }
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Creates a new State for a TokenizerFSM.
		/// </summary>
		/// <param name="action">The action to perform when this state is entered.</param>
		/// <param name="defaultTransition">The default state to move to if none of the transition rules return true.</param>
		/// <param name="eofTransition">The state to move to when the end of the stream is reached.</param>
		/// <param name="transitions">The target states that may be moved to from this state along with the conditions that determine when to move to those states.</param>
		public State(StateAction action, int defaultTransition, int eofTransition, params Transition[] transitions)
		{
			if(transitions == null)
				throw new ArgumentNullException("transitions");

			_action = action;
			_defaultTransition = defaultTransition;
			_eofTransition = eofTransition;
			_transitions = (Transition[])transitions.Clone();
			_readOnlyTransitions = Array.AsReadOnly(_transitions);
			_transitionCache[-1] = eofTransition;
		}

		#endregion
		#region Public Methods

		/// <summary>
		/// Gets the next state that cooresponds to the specified input character.
		/// </summary>
		public int GetNextState(int input)
		{
			int transition;
			if(!_transitionCache.TryGetValue(input, out transition))
			{
				transition = _defaultTransition;
				foreach(Transition t in _transitions)
				{
					if(t.Condition(input))
						transition = t.Target;
				}
				_transitionCache.Add(input, transition);
			}
			return transition;
		}

		#endregion
	}
}