using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Runs at the target until within the stop distance. Close in, it needs its melee turn
    // (only a few allies melee one target at once) and circles at a distance until it gets
    // one. Holds still while an attack winds up, and at the end of its path if the target is
    // somewhere it can't reach. Fails when the target is no longer detected.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Chase Target", "Runs toward the target until within the distance (0 = attack range).")]
    public sealed class ChaseTarget : EnemyAction
    {
        private const float TurnRing     = 3f;    // metres beyond attack range where it needs its turn
        private const float CircleRadius = 2.5f;  // metres beyond attack range it waits at

        [Tooltip("Metres. 0 = EnemyData's attack range")]
        [SerializeField, Min(0f)] private float _stopDistance;

        public override string Summary => _stopDistance > 0f ? $"Until {_stopDistance:0.#} m" : null;

        protected override BtStatus OnTick(BtContext context)
        {
            var perception = Ai.Perception;
            if (!perception.IsTargetDetected) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Chase);
            Vector3 targetPos = perception.Target.transform.position;

            if (Ai.IsAttacking)
            {
                Ai.Agent.isStopped = true;
                Ai.FaceTowards(targetPos, context.DeltaTime);
                return BtStatus.Running;
            }

            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            float range    = Ai.Data.AttackRange;
            float current  = Vector3.Distance(targetPos, Ai.transform.position);
            if (current <= range + TurnRing && !Ai.Tactics.TryTakeMeleeTurn(perception.Target))
            {
                Ai.Tactics.CircleTarget(targetPos, range + CircleRadius);
                Ai.FaceTowards(targetPos, context.DeltaTime);
                Ai.ReportStep(EngageStep.Circle);
                return BtStatus.Running;
            }

            float distance = _stopDistance > 0f ? _stopDistance : range;
            if (current <= distance) return BtStatus.Success;

            Ai.Agent.isStopped = false;
            Ai.Agent.SetDestination(targetPos);
            if (Ai.IsPathBlocked)
            {
                Ai.Agent.isStopped = true;
                Ai.FaceTowards(targetPos, context.DeltaTime);
                Ai.ReportStep(EngageStep.Hold);
                return BtStatus.Running;
            }

            Ai.ReportStep(EngageStep.Approach);
            return BtStatus.Running;
        }
    }
}
