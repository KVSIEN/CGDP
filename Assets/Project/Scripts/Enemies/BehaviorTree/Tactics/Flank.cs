using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Circles round to the target's side by the route it is least likely to see, instead of
    // running straight at it. Goes for where it believes the target is, so a target that
    // moves meanwhile may be gone when it gets there. Put a Chance and a Cooldown above it so
    // it only flanks sometimes. Succeeds on arrival; fails when it finds no route.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Flank", "Circles round to the target's side by the least visible route instead of charging.")]
    public sealed class Flank : EnemyAction
    {
        private const float ArrivalDistance = 0.8f;

        [Tooltip("Metres from the target to end up at. 0 = a little beyond attack range (melee) or the preferred range (ranged)")]
        [SerializeField, Min(0f)] private float _standOff;
        [Tooltip("Give up if not there after this many seconds")]
        [SerializeField, Min(1f)] private float _giveUpAfter = 8f;

        [NonSerialized] private bool  _moving;
        [NonSerialized] private float _givesUpAt;

        public override string Summary => _standOff > 0f ? $"{_standOff:0.#} m off" : null;

        protected override void OnEnter(BtContext context)
        {
            _givesUpAt = context.Time + _giveUpAfter;
            _moving = Ai.Perception.Target != null &&
                      Ai.Tactics.TryPlanFlank(Ai.Perception.TargetPosition, StandOff(), out Vector3 destination, SquadSide()) &&
                      Ai.Agent.SetDestination(destination);
            if (_moving) Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_moving || context.Time >= _givesUpAt) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Chase);
            Ai.ReportStep(EngageStep.Flank);
            Ai.SetSpeed(Ai.Data.ChaseSpeed);
            return HasArrived(Ai.Agent, ArrivalDistance) ? BtStatus.Success : BtStatus.Running;
        }

        // Squad flankers split left and right; anyone else picks a side at random.
        private int SquadSide() => Ai.SquadRole == SquadRole.Flank ? Ai.Squad.NextFlankSide() : 0;

        private float StandOff()
        {
            if (_standOff > 0f) return _standOff;
            return Ai.Data.CombatType == EnemyCombatType.Ranged ? Ai.Data.PreferredRange : Ai.Data.AttackRange + 2f;
        }
    }
}
