using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while the target is out of sight but it still has a working idea of where it went
    // (Enemy Belief): a likelihood over the cover round where it was last seen, fading with time.
    // Put it in front of Pursue Belief so a lost target is followed up on in order, not searched
    // for from scratch.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Has Belief", "Succeeds while the target is unseen but it still has a belief about where it went.")]
    public sealed class HasBelief : EnemyCondition
    {
        protected override bool Check(BtContext context) =>
            !Ai.Perception.IsTargetDetected && Ai.Belief.IsActive;
    }
}
