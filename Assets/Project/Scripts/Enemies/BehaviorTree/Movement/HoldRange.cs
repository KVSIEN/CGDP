using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // A ranged enemy's footwork: closes in, backs off or strafes to stay at EnemyData's
    // preferred range, standing still while a burst goes out. Keeps running while the
    // target is detected — run it in a Parallel next to Shoot Burst.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Hold Range", "Keeps the preferred distance from the target, strafing while there. Runs until the target is lost.")]
    public sealed class HoldRange : EnemyAction
    {
        protected override void OnEnter(BtContext context) => Ai.RangedCombat.ResetSpacing();

        protected override BtStatus OnTick(BtContext context)
        {
            var perception = Ai.Perception;
            if (!perception.IsTargetDetected) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Chase);
            Ai.ReportStep(EngageStep.KeepDistance);
            var targetPos = perception.Target.transform.position;
            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            Ai.FaceTowards(targetPos, context.DeltaTime);

            if (Ai.RangedCombat.IsBursting) Ai.Agent.isStopped = true;
            else                             Ai.RangedCombat.HoldPreferredRange(targetPos, context.DeltaTime);
            return BtStatus.Running;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;
    }
}
