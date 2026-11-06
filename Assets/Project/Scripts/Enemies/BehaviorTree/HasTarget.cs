using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Has Target", "Succeeds while a hostile is seen or within proximity range right now.")]
    public sealed class HasTarget : EnemyCondition
    {
        protected override bool Check(BtContext context) => Ai.Perception.IsTargetDetected;
    }
}
