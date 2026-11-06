using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while it senses its target, or lost track of it only moments ago — long enough
    // to keep a flank going around a corner without the fight dropping to a search.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Knows Target", "Succeeds while it senses its target, or lost it only moments ago.")]
    public sealed class KnowsTarget : EnemyCondition
    {
        [Tooltip("Seconds after losing sight that still count")]
        [SerializeField, Min(0f)] private float _memory = 4f;

        public override string Summary => $"{_memory:0.#} s memory";

        protected override bool Check(BtContext context)
        {
            var perception = Ai.Perception;
            if (perception.IsTargetDetected) return true;
            return perception.Target != null && perception.Level != AwarenessLevel.Unaware &&
                   perception.TimeSinceSensed <= _memory;
        }
    }
}
