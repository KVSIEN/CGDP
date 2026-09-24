using UnityEngine;

namespace CGD.Quests
{
    // Something happened that objectives may count: an enemy of a type died, an item
    // was picked up, a signal was raised.
    public readonly struct QuestEvent
    {
        public QuestEvent(ObjectiveKind kind, Object target, int amount)
        {
            Kind   = kind;
            Target = target;
            Amount = amount;
        }

        public ObjectiveKind Kind   { get; }
        public Object        Target { get; }
        public int           Amount { get; }
    }
}
