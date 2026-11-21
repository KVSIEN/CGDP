using System;
using UnityEngine;
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
            if (Ai.Perception.Target == null) return;

            _hasDestination = Ai.Routing.RunFrom(Ai.Perception.TargetPosition, _distance);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_hasDestination) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Retreat);
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed);
            return HasArrived(Ai.Agent, ArrivalDistance) ? BtStatus.Success : BtStatus.Running;
        }
    }
}
