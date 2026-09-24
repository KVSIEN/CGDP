using System.Collections.Generic;
using CGD.Core;

namespace CGD.Quests
{
    // Runtime state of one quest. Which state moves are legal is a StateMachine rule, so
    // a completed quest can't be failed and a locked one can't be started.
    public class QuestProgress
    {
        private readonly StateMachine<QuestState> _state = new();
        private readonly List<ObjectiveProgress>  _objectives = new();

        public QuestProgress(QuestDefinition definition)
        {
            Definition = definition;
            foreach (ObjectiveDefinition objective in definition.Objectives)
                if (objective != null) _objectives.Add(new ObjectiveProgress(objective));

            _state.Add(QuestState.Locked)
                  .Add(QuestState.Available)
                  .Add(QuestState.Active)
                  .Add(QuestState.Completed)
                  .Add(QuestState.Failed)
                  .SetRule(IsAllowed);
            _state.Start(QuestState.Locked);
        }

        public QuestDefinition Definition { get; }
        public QuestState      State      => _state.Current;
        public bool            IsActive   => State == QuestState.Active;

        public IReadOnlyList<ObjectiveProgress> Objectives => _objectives;

        // Game seconds left on a timed quest while it's active.
        public float TimeRemaining { get; private set; }

        // Required objectives all done (optional ones don't matter).
        public bool RequiredComplete
        {
            get
            {
                foreach (ObjectiveProgress objective in _objectives)
                    if (!objective.Definition.IsOptional && !objective.IsComplete) return false;
                return true;
            }
        }

        // An objective is counting when the quest is active and, for a sequential quest,
        // every required objective before it is done. Optional ones always count.
        public bool IsListening(ObjectiveProgress objective)
        {
            if (!IsActive || objective.IsComplete) return false;
            if (!Definition.Sequential || objective.Definition.IsOptional) return true;

            foreach (ObjectiveProgress earlier in _objectives)
            {
                if (earlier == objective) return true;
                if (!earlier.Definition.IsOptional && !earlier.IsComplete) return false;
            }
            return false;
        }

        internal bool TryEnter(QuestState next) => _state.TryChangeTo(next);

        internal void Begin()
        {
            foreach (ObjectiveProgress objective in _objectives)
                objective.Reset();
            TimeRemaining = Definition.TimeLimit;
        }

        // Returns true when any objective's count changed.
        internal bool Report(in QuestEvent e)
        {
            bool progressed = false;
            foreach (ObjectiveProgress objective in _objectives)
                if (objective.Definition.Counts(e) && IsListening(objective))
                    progressed |= objective.Add(e.Amount);
            return progressed;
        }

        // Returns true when the time limit just ran out.
        internal bool TickTimer(float deltaTime)
        {
            if (!IsActive || !Definition.HasTimeLimit) return false;

            TimeRemaining -= deltaTime;
            return TimeRemaining <= 0f;
        }

        private static bool IsAllowed(QuestState from, QuestState to) => from switch
        {
            QuestState.Locked    => to == QuestState.Available,
            QuestState.Available => to == QuestState.Active,
            QuestState.Active    => to is QuestState.Completed or QuestState.Failed,
            QuestState.Failed    => to == QuestState.Available,
            _                    => false,
        };
    }
}
