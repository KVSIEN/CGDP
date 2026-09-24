using System;
using System.Collections.Generic;

namespace CGD.Core
{
    // Reusable finite state machine keyed by an enum (Idle, Alert, Attack, Stunned, Dead…).
    // Plain C#: the owner decides what drives Tick (Update, FixedUpdate or GameClock).
    //
    // Three ways to move between states, usable together:
    //   - transitions: conditions checked every Tick, from one state or from any state
    //     ("any → Stunned while stunned" is one line instead of a check in every state)
    //   - TryChangeTo: a state or outside event asks directly
    //   - ForceChangeTo: resets and respawns, ignoring the rule and re-entering if needed
    // An optional rule says which moves are legal at all; illegal requests are refused.
    //
    // A state id doesn't need behaviour: ids added without an IState are valid states
    // that do nothing (useful for terminal states like Dead, or pure status tracking).
    public class StateMachine<TId> where TId : struct, Enum
    {
        private static readonly EqualityComparer<TId> Comparer = EqualityComparer<TId>.Default;

        private readonly Dictionary<TId, IState>           _states      = new();
        private readonly Dictionary<TId, List<Transition>> _transitions = new();
        private readonly List<Transition>                  _anyTransitions = new();

        private Func<TId, TId, bool> _rule;
        private IState _current;

        public TId   Current     { get; private set; }
        public TId   Previous    { get; private set; }
        public bool  IsRunning   { get; private set; }
        // Time passed to Tick since the current state was entered.
        public float TimeInState { get; private set; }

        // (previous, next)
        public event Action<TId, TId> Changed;

        public StateMachine<TId> Add(TId id, IState state = null)
        {
            _states[id] = state;
            return this;
        }

        public StateMachine<TId> AddTransition(TId from, TId to, Func<bool> condition)
        {
            if (!_transitions.TryGetValue(from, out List<Transition> list))
                _transitions[from] = list = new List<Transition>();

            list.Add(new Transition(to, condition));
            return this;
        }

        // Checked before the current state's own transitions, from every state but the target.
        public StateMachine<TId> AddAnyTransition(TId to, Func<bool> condition)
        {
            _anyTransitions.Add(new Transition(to, condition));
            return this;
        }

        // Which moves are legal, e.g. (from, to) => from != Dead. Applies to transitions and
        // TryChangeTo; ForceChangeTo ignores it.
        public StateMachine<TId> SetRule(Func<TId, TId, bool> isAllowed)
        {
            _rule = isAllowed;
            return this;
        }

        public bool Has(TId id) => _states.ContainsKey(id);

        public bool IsIn(TId id) => IsRunning && Comparer.Equals(Current, id);

        public void Start(TId initial)
        {
            if (IsRunning) _current?.Exit();

            IsRunning = true;
            Enter(initial, initial);
        }

        public bool CanChangeTo(TId next) =>
            IsRunning && Has(next) && !Comparer.Equals(next, Current) && (_rule == null || _rule(Current, next));

        public bool TryChangeTo(TId next)
        {
            if (!CanChangeTo(next)) return false;

            ChangeTo(next);
            return true;
        }

        public void ForceChangeTo(TId next)
        {
            if (!Has(next)) throw new ArgumentException($"State {next} was never added.", nameof(next));
            if (!IsRunning)
            {
                Start(next);
                return;
            }

            ChangeTo(next);
        }

        // Takes at most one transition, then ticks whichever state is current.
        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            TimeInState += deltaTime;

            if (!TryTransition(_anyTransitions) && _transitions.TryGetValue(Current, out List<Transition> own))
                TryTransition(own);

            _current?.Tick(deltaTime);
        }

        private bool TryTransition(List<Transition> transitions)
        {
            foreach (Transition transition in transitions)
            {
                if (!CanChangeTo(transition.To) || !transition.Condition()) continue;

                ChangeTo(transition.To);
                return true;
            }
            return false;
        }

        private void ChangeTo(TId next)
        {
            _current?.Exit();
            Enter(Current, next);
        }

        private void Enter(TId previous, TId next)
        {
            Previous    = previous;
            Current     = next;
            TimeInState = 0f;

            _states.TryGetValue(next, out _current);
            _current?.Enter();
            Changed?.Invoke(previous, next);
        }

        private readonly struct Transition
        {
            public Transition(TId to, Func<bool> condition)
            {
                To        = to;
                Condition = condition;
            }

            public TId        To        { get; }
            public Func<bool> Condition { get; }
        }
    }
}
