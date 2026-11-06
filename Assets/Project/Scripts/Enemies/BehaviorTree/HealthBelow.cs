using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Health Below", "Succeeds while health (shield not counted) is at or below the fraction.")]
    public sealed class HealthBelow : EnemyCondition
    {
        [SerializeField, Range(0f, 1f)] private float _fraction = 0.3f;

        public override string Summary => $"≤ {_fraction:P0}";

        protected override bool Check(BtContext context)
        {
            var health = Ai.Health;
            return health.MaxHealth > 0f && health.Health / health.MaxHealth <= _fraction;
        }
    }
}
