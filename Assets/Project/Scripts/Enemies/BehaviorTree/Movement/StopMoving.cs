using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Stop Moving", "Clears the enemy's path so it stands still, then succeeds.")]
    public sealed class StopMoving : EnemyAction
    {
        protected override BtStatus OnTick(BtContext context)
        {
            Ai.Routing.Stop();
            return BtStatus.Success;
        }
    }
}
