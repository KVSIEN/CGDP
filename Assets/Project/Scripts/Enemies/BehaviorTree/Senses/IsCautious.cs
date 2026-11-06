using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Is Cautious", "Succeeds while it is on edge after a search that found nothing.")]
    public sealed class IsCautious : EnemyCondition
    {
        protected override bool Check(BtContext context) => Ai.Perception.IsCautious;
    }
}
