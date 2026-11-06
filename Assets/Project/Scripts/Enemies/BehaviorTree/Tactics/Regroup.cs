using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Falls back to high cover from the (believed) target, or to its post, and holds there
    // facing the threat until its morale recovers. Succeeds once rallied; fails if cornered
    // (put it under Is Shaken so the fight takes over).
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Regroup", "Falls back to cover or its post and holds until morale recovers. Succeeds once rallied.")]
    public sealed class Regroup : EnemyAction
    {
        private const float ArrivalDistance = 0.8f;

        protected override void OnEnter(BtContext context) => RetreatStep.Begin(Ai);

        protected override BtStatus OnTick(BtContext context)
        {
            if (!Ai.Morale.IsShaken) return BtStatus.Success;
            if (Ai.IsCornered) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Retreat);
            RetreatStep.Tick(Ai, context.DeltaTime, ArrivalDistance);
            return BtStatus.Running;
        }

        protected override void OnExit(BtContext context)
        {
            Ai.Agent.isStopped = false;
            Ai.Tactics.ReleaseCover();
        }
    }
}
