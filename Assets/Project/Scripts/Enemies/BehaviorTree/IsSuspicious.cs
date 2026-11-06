using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Something is worth looking into: it glimpsed, heard or felt something, was hit, heard
    // an ally call out — or lost sight of the target it was fighting.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Is Suspicious", "Succeeds while it is suspicious or hunting (noticed something, was hit, lost its target).")]
    public sealed class IsSuspicious : EnemyCondition
    {
        protected override bool Check(BtContext context)
        {
            var perception = Ai.Perception;
            return perception.HasLead
                || perception.Level == AwarenessLevel.Suspicious
                || perception.Level == AwarenessLevel.Searching;
        }
    }
}
