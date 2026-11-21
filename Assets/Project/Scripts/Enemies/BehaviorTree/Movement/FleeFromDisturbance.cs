using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Bolts from whatever startled it: the target if it has one, otherwise the spot a noise or
    // a glimpse sent it looking at (the lead). Unlike Flee it needs no target, so a skittish
    // creature runs from a gunshot it only heard. Succeeds on arrival, with the lead spent so
    // the same sound doesn't send it running twice; fails when there is nothing to run from
    // or nowhere to go.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Flee From Disturbance", "Runs away from the target, or from the last noise or glimpse if it has no target. Succeeds on arrival.")]
    public sealed class FleeFromDisturbance : EnemyAction
    {
        private const float ArrivalDistance = 0.6f;

        [SerializeField, Min(1f)] private float _distance = 14f;

        [NonSerialized] private bool _hasDestination;

        public override string Summary => $"{_distance:0.#} m";

        protected override void OnEnter(BtContext context)
        {
            _hasDestination = false;
            var perception = Ai.Perception;

            Vector3 from = perception.Target != null ? perception.TargetPosition : perception.LookPosition;
            perception.ConsumeLead();
            _hasDestination = Ai.Routing.RunFrom(from, _distance);
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
