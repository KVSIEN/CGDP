using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Target In Range", "Succeeds while the target is within the range (0 = the enemy's attack range).")]
    public sealed class TargetInRange : EnemyCondition
    {
        [Tooltip("Metres. 0 = EnemyData's attack range")]
        [SerializeField, Min(0f)] private float _range;

        public override string Summary => _range > 0f ? $"≤ {_range:0.#} m" : "Attack range";

        protected override bool Check(BtContext context)
        {
            var target = Ai.Perception.Target;
            if (target == null) return false;

            float range = _range > 0f ? _range : Ai.Data.AttackRange;
            return (target.transform.position - Ai.transform.position).sqrMagnitude <= range * range;
        }
    }
}
