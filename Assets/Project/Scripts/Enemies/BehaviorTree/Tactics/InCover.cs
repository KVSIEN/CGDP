using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while it stands at its cover spot and the cover still stands between it and
    // where it believes the target is — fails once the target has worked round it.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/In Cover", "Succeeds while it is at its cover and the cover still faces the (believed) target.")]
    public sealed class InCover : EnemyCondition
    {
        protected override bool Check(BtContext context) =>
            Ai.Perception.Target != null && Ai.Tactics.IsInCover(Ai.Perception.TargetPosition);
    }
}
