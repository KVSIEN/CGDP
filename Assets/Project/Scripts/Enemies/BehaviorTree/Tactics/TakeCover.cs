using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Runs to the best cover it knows against where it believes the target is — a different
    // spot each time, so running it again repositions. Succeeds on arrival; fails when it
    // knows of no suitable cover.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Take Cover", "Runs to known cover from the (believed) target, a new spot each time. Fails if it knows none.")]
    public sealed class TakeCover : EnemyAction
    {
        private const float ArrivalDistance = 0.6f;

        [Tooltip("Fight: somewhere to shoot from near its preferred range. Hide: out of sight (high cover only). Advance: cover closer to the target, to close in")]
        [SerializeField] private CoverUse _use = CoverUse.Fight;
        [Tooltip("Give up if not there after this many seconds")]
        [SerializeField, Min(1f)] private float _giveUpAfter = 6f;

        [NonSerialized] private bool  _moving;
        [NonSerialized] private float _givesUpAt;

        public override string Summary => _use.ToString();

        protected override void OnEnter(BtContext context)
        {
            _givesUpAt = context.Time + _giveUpAfter;
            _moving = Ai.Perception.Target != null &&
                      Ai.Tactics.TryTakeCover(Ai.Perception.TargetPosition, _use, out Vector3 destination) &&
                      Ai.Agent.SetDestination(destination);
            if (_moving) Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_moving || context.Time >= _givesUpAt) return BtStatus.Failure;

            Ai.ReportState(_use == CoverUse.Hide ? EnemyAI.AiState.Retreat : EnemyAI.AiState.Chase);
            Ai.ReportStep(EngageStep.MoveToCover);
            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            return HasArrived(Ai.Agent, ArrivalDistance) ? BtStatus.Success : BtStatus.Running;
        }
    }
}
