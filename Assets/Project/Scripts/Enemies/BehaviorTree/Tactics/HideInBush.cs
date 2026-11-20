using System;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Core;

namespace CGD.Enemies
{
    // Breaks off and goes to ground in a bush: runs (straight, not through other bushes) to one
    // it knows a good way from its target and not past it — going round to the side that keeps
    // the bush between them, then in to the far side of it — then lies still inside, eyes on where the
    // target is, for a while. A bush hides it from sight, so it comes out of this knowing where
    // the target is while the target has lost it — the moment to start stalking again.
    // Succeeds once it has lain low for the hold time; fails when there is no such bush within
    // reach or it can't get there in time (put Take Cover or Flee after it).
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Hide In Bush", "Runs to a bush away from the target and lies low in it, watching. Fails when no bush is in reach.")]
    public sealed class HideInBush : EnemyAction
    {
        private const float ArrivalDistance = 0.8f;

        [Tooltip("Metres the bush must be from the target")]
        [SerializeField, Min(0f)] private float _minFromTarget = 9f;
        [Tooltip("Metres it will run to reach one")]
        [SerializeField, Min(1f)] private float _maxRun = 25f;
        [Tooltip("Seconds it lies low once there (random within the range)")]
        [SerializeField] private FloatRange _hold = new(2f, 3.5f);
        [Tooltip("Speed on the way, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 1f;
        [Tooltip("Seconds to reach the bush before giving up")]
        [SerializeField, Min(0.5f)] private float _giveUpAfter = 6f;

        [NonSerialized] private bool        _holding;
        [NonSerialized] private float       _until;
        [NonSerialized] private bool        _goingRound;   // still on the way to the point outside the bush
        [NonSerialized] private Vector3     _spot;

        public override string Summary => $"{_minFromTarget:0}+ m from target, {_hold.Min:0.#}–{_hold.Max:0.#} s";

        protected override void OnEnter(BtContext context)
        {
            _holding = false;
            _until   = context.Time + _giveUpAfter;
            if (!HidingBush.TryPickRoute(Ai, Ai.Perception.TargetPosition, _minFromTarget, _maxRun, out BushRoute route) || !Ai.Routing.SetDestination(route.First))
            {
                _until = float.NegativeInfinity;   // nowhere to go: fail on the first tick
                return;
            }
            _spot      = route.Spot;
            _goingRound = route.HasVia;
            Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (_holding) return Hold(context);
            if (context.Time >= _until) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Retreat);
            Ai.ReportStep(EngageStep.MoveToCover);
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            if (!HasArrived(Ai.Agent, ArrivalDistance)) return BtStatus.Running;

            if (_goingRound)
            {
                // Round the outside: now in, to where it lies.
                _goingRound = false;
                Ai.Routing.SetDestination(_spot);
                return BtStatus.Running;
            }

            Ai.Routing.Stop();
            _holding = true;
            _until   = context.Time + _hold.EvaluateClamped();
            return BtStatus.Running;
        }

        private BtStatus Hold(BtContext context)
        {
            Ai.ReportState(EnemyAI.AiState.Chase);   // still in the fight, so the Hold step shows
            Ai.ReportStep(EngageStep.Hold);
            Ai.Motion.KeepEyesOnTarget(context.DeltaTime);
            return context.Time >= _until ? BtStatus.Success : BtStatus.Running;
        }
    }
}
