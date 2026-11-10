using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Withdraws to a planned fallback position: cover that screens it from the (believed) target,
    // well away from every hostile it knows of, reached without running past them, and near home
    // ground. Plans again as the situation changes. Succeeds on arrival; fails when it knows of
    // no such spot, so a Selector can fall through to simply fleeing.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Fall Back", "Withdraws to a planned fallback: screened cover far from every known hostile, by a route that doesn't pass them. Fails if none.")]
    public sealed class FallBack : EnemyAction
    {
        private const float ArrivalDistance = 0.6f;

        [Tooltip("Give up if not there after this many seconds")]
        [SerializeField, Min(1f)] private float _giveUpAfter = 8f;

        [NonSerialized] private bool  _moving;
        [NonSerialized] private float _givesUpAt;

        protected override void OnEnter(BtContext context)
        {
            _givesUpAt = context.Time + _giveUpAfter;
            _moving = Ai.Perception.Target != null &&
                      Ai.Tactics.TryFallBack(Ai.Perception.TargetPosition, out Vector3 destination) &&
                      Ai.Routing.SetDestination(destination);
            if (_moving) Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_moving || context.Time >= _givesUpAt) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Retreat);
            Ai.ReportStep(EngageStep.MoveToCover);
            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            Ai.KeepEyesOnTarget(context.DeltaTime);
            return HasArrived(Ai.Agent, ArrivalDistance) ? BtStatus.Success : BtStatus.Running;
        }
    }
}
