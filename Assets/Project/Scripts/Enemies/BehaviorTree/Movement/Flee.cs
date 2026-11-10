using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Runs away from the target to a point the given distance off. Succeeds on arrival;
    // fails when there's nothing to run from or nowhere to go.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Flee", "Runs away from the target. Succeeds once the distance is covered.")]
    public sealed class Flee : EnemyAction
    {
        private const float ArrivalDistance = 0.6f;

        [SerializeField, Min(1f)] private float _distance = 8f;

        [NonSerialized] private bool _hasDestination;

        public override string Summary => $"{_distance:0.#} m";

        protected override void OnEnter(BtContext context)
        {
            _hasDestination = false;
            var target = Ai.Perception.Target;
            if (target == null) return;

            Vector3 away = Ai.transform.position - Ai.Perception.TargetPosition;
            away.y = 0f;
            Vector3 direction = away.sqrMagnitude > 0.01f ? away.normalized : -Ai.transform.forward;
            Vector3 goal      = Ai.transform.position + direction * _distance;

            if (!NavMesh.SamplePosition(goal, out var hit, _distance * 0.5f, NavMesh.AllAreas)) return;

            Ai.Agent.isStopped = false;
            _hasDestination = Ai.Routing.SetDestination(hit.position);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_hasDestination) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Retreat);
            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            return HasArrived(Ai.Agent, ArrivalDistance) ? BtStatus.Success : BtStatus.Running;
        }
    }
}
