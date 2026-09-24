using UnityEngine;

namespace CGD.Quests
{
    public class ObjectiveProgress
    {
        public ObjectiveProgress(ObjectiveDefinition definition) => Definition = definition;

        public ObjectiveDefinition Definition { get; }
        public int  Count      { get; private set; }
        public bool IsComplete => Count >= Definition.RequiredCount;

        // Returns true when the count changed.
        internal bool Add(int amount)
        {
            if (IsComplete || amount <= 0) return false;

            Count = Mathf.Min(Count + amount, Definition.RequiredCount);
            return true;
        }

        internal void Reset() => Count = 0;
    }
}
