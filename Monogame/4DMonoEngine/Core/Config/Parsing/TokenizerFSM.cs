using System;
using System.Collections.ObjectModel;

namespace _4DMonoEngine.Core.Config.Parsing
{
	/// <summary>
	/// A finite state machine used to parse input streams into sequences of tokens.
	/// </summary>
	public class TokenizerFSM
	{
		#region Private Fields

		private readonly State[] _states;
		private readonly ReadOnlyCollection<State> _readOnlyStates;

		#endregion
		#region Constructors

		/// <summary>
		/// Creates an FSM which parses input streams using the specified states.
		/// </summary>
		public TokenizerFSM(params State[] states)
		{
			if(states == null || states.Length == 0)
				throw new ArgumentException("A TokenizerFSM must be constructed with at least one State.");

			ValidateTransitions(states);
			_states = (State[])states.Clone();
			_readOnlyStates = Array.AsReadOnly(_states);
		}

		#endregion
		#region Constructors

		/// <summary>
		/// Gets a read-only collection representing the states in this parser.
		/// </summary>
		public ReadOnlyCollection<State> States
		{
			get { return _readOnlyStates; }
		}

		#endregion
		#region Private Static Methods

		/// <summary>
		/// Validates the transitions for the specified states.
		/// </summary>
		private static void ValidateTransitions(State[] states)
		{
			for(int i = 0; i < states.Length; i++)
			{
				State s = states[i];
				if(s.Transitions != null)
				{
					for(int j = 0; j < s.Transitions.Count; j++)
					{
						Transition t = s.Transitions[j];
						if((t.Target < 0 || t.Target >= states.Length) && t.Target != Transition.Return && t.Target != Transition.Error)
							throw new ArgumentException("Target of transition " + j + " of state " + i + " is out of range.");
					}
				}

				if((s.DefaultTransition < 0 || s.DefaultTransition >= states.Length) && s.DefaultTransition != Transition.Return
				   && s.DefaultTransition != Transition.Error)
					throw new ArgumentException("Default transition for state " + i + " is out of range.");

				if((s.EOFTransition < 0 || s.EOFTransition >= states.Length) && s.EOFTransition != Transition.Return
				   && s.EOFTransition != Transition.Error)
					throw new ArgumentException("EOF transition for state " + i + " is out of range.");
			}
		}

		#endregion
	}
}