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
            if (Ai.Agent.isOnNavMesh) Ai.Agent.ResetPath();
            return BtStatus.Success;
        }
    }
}
