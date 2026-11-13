using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Calls its kin to it over the radio, walls or not: everyone of its side in range hears where
    // its target is and comes looking (a loner never calls). Succeeds at once — put a Cooldown
    // above it.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Call For Help", "Radios its kin where its target is, so they converge. Succeeds at once.")]
    public sealed class CallForHelp : EnemyAction
    {
        protected override BtStatus OnTick(BtContext context)
        {
            if (Ai.Perception.Target == null) return BtStatus.Failure;
            Ai.Perception.CallForHelp();
            return BtStatus.Success;
        }
    }
}
