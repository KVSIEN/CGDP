using System;
using System.Collections.Generic;

namespace CGD.Quests
{
    // Every quest the player knows about and the rules that move them along. Plain C#:
    // QuestTracker feeds it events and game time and hands out rewards.
    //
    // Availability cascades: completing a quest unlocks every quest whose prerequisites
    // are now all complete, and auto-start quests start immediately — that's a chain.
    public class QuestLog
    {
        private readonly List<QuestProgress> _quests = new();

        public IReadOnlyList<QuestProgress> Quests => _quests;

        // State or objective progress changed.
        public event Action<QuestProgress> QuestChanged;
        public event Action<QuestProgress> QuestCompleted;
        public event Action<QuestProgress> QuestFailed;

        public QuestProgress Get(QuestDefinition definition) => _quests.Find(q => q.Definition == definition);

        public void Add(QuestDefinition definition)
        {
            if (definition == null || Get(definition) != null) return;

            _quests.Add(new QuestProgress(definition));
            RefreshAvailability();
        }

        public bool Start(QuestDefinition definition)
        {
            QuestProgress quest = Get(definition);
            return quest != null && Start(quest);
        }

        public bool Fail(QuestDefinition definition)
        {
            QuestProgress quest = Get(definition);
            return quest != null && Fail(quest);
        }

        public void Report(QuestEvent e)
        {
            // Completing a quest can start others; they only count events from now on.
            for (int i = 0; i < _quests.Count; i++)
            {
                QuestProgress quest = _quests[i];
                if (!quest.IsActive || !quest.Report(e)) continue;

                QuestChanged?.Invoke(quest);
                if (quest.RequiredComplete) Complete(quest);
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _quests.Count; i++)
                if (_quests[i].TickTimer(deltaTime))
                    Fail(_quests[i]);
        }

        // A quest with no required objectives completes the moment it starts — useful as
        // a pure story beat or a gate in a chain.
        private bool Start(QuestProgress quest)
        {
            if (!quest.TryEnter(QuestState.Active)) return false;

            quest.Begin();
            QuestChanged?.Invoke(quest);
            if (quest.RequiredComplete) Complete(quest);
            return true;
        }

        private void Complete(QuestProgress quest)
        {
            if (!quest.TryEnter(QuestState.Completed)) return;

            QuestChanged?.Invoke(quest);
            QuestCompleted?.Invoke(quest);
            RefreshAvailability();
        }

        private bool Fail(QuestProgress quest)
        {
            if (!quest.TryEnter(QuestState.Failed)) return false;

            QuestChanged?.Invoke(quest);
            QuestFailed?.Invoke(quest);

            // A retryable auto-start quest therefore restarts straight away, timer reset.
            if (quest.Definition.Retryable && quest.TryEnter(QuestState.Available))
            {
                QuestChanged?.Invoke(quest);
                RefreshAvailability();
            }
            return true;
        }

        // Repeats until nothing changes, so a chain of auto-start quests with no
        // objectives resolves in one go.
        private void RefreshAvailability()
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < _quests.Count; i++)
                    changed |= Advance(_quests[i]);
            }
        }

        private bool Advance(QuestProgress quest)
        {
            switch (quest.State)
            {
                case QuestState.Locked when PrerequisitesMet(quest.Definition):
                    quest.TryEnter(QuestState.Available);
                    QuestChanged?.Invoke(quest);
                    return true;

                case QuestState.Available when quest.Definition.AutoStart:
                    return Start(quest);

                default:
                    return false;
            }
        }

        // A prerequisite this log doesn't know about can never complete, so it blocks.
        private bool PrerequisitesMet(QuestDefinition definition)
        {
            foreach (QuestDefinition prerequisite in definition.Prerequisites)
            {
                if (prerequisite == null) continue;
                if (Get(prerequisite)?.State != QuestState.Completed) return false;
            }
            return true;
        }
    }
}
