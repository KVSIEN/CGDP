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
        [Tooltip("When above 0, the range is this multiple of the enemy's engagement range instead (EnemyData › Preferred Range for a ranged enemy, attack range + 2 m for a melee one), so one shared behavior suits a close, mid or long range fighter")]
        [SerializeField, Min(0f)] private float _engagementShare;

        public override string Summary =>
            _engagementShare > 0f ? $"≤ {_engagementShare:0.##} × engagement range" : _range > 0f ? $"≤ {_range:0.#} m" : "Attack range";

        protected override bool Check(BtContext context)
        {
            if (Ai.Perception.Target == null) return false;

            // Measured to where it believes the target is: it can't know about a move it didn't see.
            float range = _engagementShare > 0f ? _engagementShare * Ai.Data.EngagementRange : _range > 0f ? _range : Ai.Data.AttackRange;
            return (Ai.Perception.TargetPosition - Ai.transform.position).sqrMagnitude <= range * range;
        }
    }
}
