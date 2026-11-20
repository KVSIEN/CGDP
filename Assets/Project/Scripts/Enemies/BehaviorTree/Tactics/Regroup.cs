using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Falls back to high cover from the (believed) target, or to its post, and holds there
    // facing the threat until its morale recovers. Succeeds once rallied; fails if cornered
    // (put it under Is Shaken so the fight takes over) or once it has lost the threat
    // altogether — with nothing left to face it would otherwise hold there, still shaken, for
    // as long as its nerve takes to come back.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Regroup", "Falls back to cover or its post and holds until morale recovers. Succeeds once rallied.")]
    public sealed class Regroup : EnemyAction
    {
        private const float ArrivalDistance = 0.8f;

        protected override void OnEnter(BtContext context) => RetreatStep.Begin(Ai);

        protected override BtStatus OnTick(BtContext context)
        {
            if (!Ai.Morale.IsShaken) return BtStatus.Success;
            if (Ai.IsCornered || LostThreat) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Retreat);
            RetreatStep.Tick(Ai, context.DeltaTime, ArrivalDistance);
            return BtStatus.Running;
        }

        private bool LostThreat => Ai.Perception.Target == null || Ai.Perception.Level == AwarenessLevel.Unaware;

        protected override void OnExit(BtContext context)
        {
            Ai.Agent.isStopped = false;
            Ai.Tactics.ReleaseCover();
        }
    }
}
