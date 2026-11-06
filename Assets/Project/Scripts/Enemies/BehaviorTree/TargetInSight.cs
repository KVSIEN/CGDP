using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Target In Sight", "Succeeds when nothing blocks a shot at the target.")]
    public sealed class TargetInSight : EnemyCondition
    {
        protected override bool Check(BtContext context)
        {
            var target = Ai.Perception.Target;
            return target != null && Ai.RangedCombat.HasLineOfSight(target.transform.position);
        }
    }
}
