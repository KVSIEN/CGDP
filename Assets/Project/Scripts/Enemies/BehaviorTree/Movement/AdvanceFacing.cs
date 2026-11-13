using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Closes in without ever turning its front away: walks at the target (where it believes it
    // is) at a measured pace, facing it the whole way, sidestepping and backpedalling as the
    // path needs — a shield bot keeping its shield up, a juggernaut that never shows its back.
    // Succeeds at the stop distance — or, with Hold On Arrival, stays there facing the target
    // (following it as it moves) until interrupted; fails without a target.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Advance Facing", "Closes in on the target at a steady pace, always facing it. Succeeds at the stop distance.")]
    public sealed class AdvanceFacing : EnemyAction
    {
        [Tooltip("Metres from the target at which it stops")]
        [SerializeField, Min(0.5f)] private float _stopDistance = 8f;
        [Tooltip("Speed, as a share of chase speed")]
        [SerializeField, Range(0.1f, 1.5f)] private float _speed = 0.5f;
        [Tooltip("At the stop distance, hold there facing the target instead of finishing (pair it in a Parallel with Shoot Burst)")]
        [SerializeField] private bool _holdOnArrival;

        public override string Summary => (_holdOnArrival ? "Holds at " : "Until ") + $"{_stopDistance:0.#} m";

        internal override bool NeverFinishes => _holdOnArrival;

        protected override BtStatus OnTick(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            if (perception.Target == null) return BtStatus.Failure;

            Vector3 target = perception.TargetPosition;
            Vector3 offset = target - Ai.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= _stopDistance * _stopDistance)
            {
                Ai.Routing.Stop();
                if (!_holdOnArrival) return BtStatus.Success;

                Ai.ReportState(EnemyAI.AiState.Chase);
                Ai.ReportStep(EngageStep.Hold);
                Ai.Motion.FaceTowards(target, context.DeltaTime);
                return BtStatus.Running;
            }

            Ai.ReportState(EnemyAI.AiState.Chase);
            Ai.ReportStep(EngageStep.Approach);
            Ai.Agent.isStopped = false;
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            Ai.Routing.SetDestination(target);
            Ai.Motion.FaceTowards(target, context.DeltaTime);
            return BtStatus.Running;
        }
    }
}
