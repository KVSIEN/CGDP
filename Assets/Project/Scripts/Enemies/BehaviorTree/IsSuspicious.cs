using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Something is worth looking into: a noise was heard, a hit came in, or the target was
    // lost from sight. Investigate clears it once the enemy has had a look.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Is Suspicious", "Succeeds after a noise or a hit, or once the target has been lost from sight.")]
    public sealed class IsSuspicious : EnemyCondition
    {
        protected override bool Check(BtContext context)
        {
            var perception = Ai.Perception;
            return perception.HasLead || (perception.Target != null && !perception.IsTargetDetected);
        }
    }
}
