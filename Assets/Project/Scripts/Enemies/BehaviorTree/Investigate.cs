using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Goes to look at the last known position (a noise, where a hit came from, where the
    // target was last seen). A new lead on the way re-routes it. Succeeds once there or
    // when time runs out, and the enemy forgets its target.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Investigate", "Walks to the last known position, then gives up and forgets the target.")]
    public sealed class Investigate : EnemyAction
    {
        private const float ArrivalDistance = 0.5f;

        [Tooltip("Seconds before giving up. 0 = EnemyData's alert duration")]
        [SerializeField, Min(0f)] private float _duration;

        [NonSerialized] private float _givesUpAt;

        public override string Summary => _duration > 0f ? $"{_duration:0.#} s" : null;

        protected override void OnEnter(BtContext context)
        {
            _givesUpAt = context.Time + (_duration > 0f ? _duration : Ai.Data.AlertDuration);
            Ai.Perception.ConsumeLead();
            GoToLead();
        }

        protected override BtStatus OnTick(BtContext context)
        {
            Ai.ReportState(EnemyAI.AiState.Alert);
            Ai.SetSpeed(Ai.Data.PatrolSpeed);

            if (Ai.Perception.ConsumeLead()) GoToLead();
            if (context.Time < _givesUpAt && !HasArrived(Ai.Agent, ArrivalDistance)) return BtStatus.Running;

            Ai.Perception.LoseTarget();
            return BtStatus.Success;
        }

        private void GoToLead()
        {
            Ai.Agent.isStopped = false;
            Ai.Agent.SetDestination(Ai.Perception.LastKnownPosition);
        }
    }
}
