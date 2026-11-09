using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Shield Below", "Succeeds while the shield is at or below the fraction of its maximum. Always succeeds with no shield.")]
    public sealed class ShieldBelow : EnemyCondition
    {
        [SerializeField, Range(0f, 1f)] private float _fraction = 0.5f;

        public override string Summary => $"≤ {_fraction:P0}";

        protected override bool Check(BtContext context)
        {
            var health = Ai.Health;
            return health.MaxShield <= 0f || health.Shield / health.MaxShield <= _fraction;
        }
    }
}
