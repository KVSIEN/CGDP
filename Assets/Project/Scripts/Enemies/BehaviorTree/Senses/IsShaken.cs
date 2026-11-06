using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Morale broke (allies went down, it got badly hurt) and it isn't cornered: time to fall back.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Is Shaken", "Succeeds while its morale is broken and it isn't cornered.")]
    public sealed class IsShaken : EnemyCondition
    {
        protected override bool Check(BtContext context) => Ai.ShouldRetreat;
    }
}
