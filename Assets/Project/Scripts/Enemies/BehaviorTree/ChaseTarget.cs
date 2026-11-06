using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Runs at the target until within the stop distance. Holds still while an attack is
    // winding up. Fails when the target is no longer detected.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Chase Target", "Runs toward the target until within the distance (0 = attack range).")]
    public sealed class ChaseTarget : EnemyAction
    {
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

            float distance = _stopDistance > 0f ? _stopDistance : Ai.Data.AttackRange;
            if ((targetPos - Ai.transform.position).sqrMagnitude <= distance * distance) return BtStatus.Success;

            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            Ai.Agent.isStopped = false;
            Ai.Agent.SetDestination(targetPos);
            return BtStatus.Running;
        }
    }
}
