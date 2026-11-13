using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    // Peekaboo: from where it hides, darts a couple of metres out to the side — to a spot with
    // a clear view of the target — head snapping round to it, fires (if it can), and is back
    // where it came from before the target can answer. Run from behind cover (Take Cover
    // Hide), so it is only ever in view for a moment, and never from quite the same place.
    // Succeeds once back; fails when there is nowhere to peek from or no target.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Peek", "Darts out of cover to a spot with a view of the target, strikes, and ducks back. Succeeds once back.")]
    public sealed class Peek : EnemyAction
    {
        private const int   Candidates      = 8;
        private const float SnapDistance    = 1f;
        private const float ArrivalDistance = 0.5f;
        private const float ChestHeight     = 1.1f;

        private enum Phase { Out, Exposed, Back }

        [Tooltip("Metres it steps out to the side (random within the range)")]
        [SerializeField] private FloatRange _stepOut = new(1.5f, 3.5f);
        [Tooltip("Longest it stays out, in seconds (random within the range); it ducks back as soon as its shot is out")]
        [SerializeField] private FloatRange _exposure = new(0.6f, 1.4f);
        [Tooltip("Speed darting out and back, as a share of chase speed")]
        [SerializeField, Range(0.5f, 2f)] private float _speed = 1.4f;
        [Tooltip("Fires a ranged burst while out (off: it only shows itself)")]
        [SerializeField] private bool _attack = true;
        [Tooltip("Seconds to get out and back before it gives up and counts it done")]
        [SerializeField, Min(0.5f)] private float _giveUpAfter = 3f;

        [NonSerialized] private Phase   _phase;
        [NonSerialized] private Vector3 _home;
        [NonSerialized] private float   _until;
        [NonSerialized] private float   _givesUpAt;
        [NonSerialized] private bool    _fired;
        [NonSerialized] private bool    _ready;

        public override string Summary => $"{_stepOut.Min:0.#}–{_stepOut.Max:0.#} m out" + (_attack ? ", strikes" : "");

        protected override void OnEnter(BtContext context)
        {
            _home      = Ai.transform.position;
            _fired     = false;
            _phase     = Phase.Out;
            _givesUpAt = context.Time + _giveUpAfter;
            _ready     = TryFindPeekSpot(out Vector3 spot) && Ai.Routing.SetDestination(spot);
            if (_ready) Ai.Agent.isStopped = false;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override void OnAbort(BtContext context) => Ai.RangedCombat.CancelBurst();

        protected override BtStatus OnTick(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            if (!_ready || perception.Target == null) return BtStatus.Failure;
            if (context.Time >= _givesUpAt) return BtStatus.Success;

            Ai.ReportState(EnemyAI.AiState.Chase);
            Ai.ReportStep(EngageStep.Peek);
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            Vector3 target = perception.TargetPosition;

            switch (_phase)
            {
                case Phase.Out:
                    Ai.Motion.KeepEyesOnTarget(context.DeltaTime);
                    if (HasArrived(Ai.Agent, ArrivalDistance) || Ai.RangedCombat.HasLineOfSight(target)) StepIntoView(target, context.Time);
                    return BtStatus.Running;

                case Phase.Exposed:
                    Ai.Motion.FaceTowards(target, context.DeltaTime);
                    if (_attack && !_fired && Ai.RangedCombat.HasLineOfSight(target)) _fired = Ai.RangedCombat.TryStartBurst();
                    bool shotOut = _attack && _fired && !Ai.RangedCombat.IsBursting;
                    if (shotOut || context.Time >= _until) DuckBack();
                    return BtStatus.Running;

                default:
                    Ai.Motion.KeepEyesOnTarget(context.DeltaTime);
                    return HasArrived(Ai.Agent, ArrivalDistance) ? BtStatus.Success : BtStatus.Running;
            }
        }

        // Out: stop dead, head snapping round to the target.
        private void StepIntoView(Vector3 target, float now)
        {
            _phase = Phase.Exposed;
            _until = now + _exposure.EvaluateClamped();
            Ai.Routing.Stop();
            if (Ai.Agent.isOnNavMesh) Ai.Agent.velocity = Vector3.zero;
            Ai.Motion.SnapFacing(target);
        }

        private void DuckBack()
        {
            _phase = Phase.Back;
            Ai.Agent.isStopped = false;
            Ai.Routing.SetDestination(_home);
        }

        // A spot a step or two to either side (or a little forward) of where it hides that has a
        // clear line to the target, on the NavMesh and out of anything that would hurt it.
        private bool TryFindPeekSpot(out Vector3 spot)
        {
            spot = default;
            Vector3 toTarget = Ai.Perception.TargetPosition - _home;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return false;

            Vector3 forward = toTarget.normalized;
            Vector3 side    = Vector3.Cross(Vector3.up, forward);
            Vector3 chest   = Ai.Perception.TargetPosition + Vector3.up * ChestHeight;
            float   first   = UnityEngine.Random.value < 0.5f ? 1f : -1f;   // which side it tries first varies

            for (int i = 0; i < Candidates; i++)
            {
                float   sign   = i % 2 == 0 ? first : -first;
                float   reach  = _stepOut.Evaluate();
                Vector3 lean   = forward * (i < Candidates / 2 ? 0f : reach * 0.5f);
                Vector3 option = _home + side * (sign * reach) + lean;

                if (!NavMesh.SamplePosition(option, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas)) continue;
                if (Hazards.DamagePerSecondAt(hit.position, Ai.Health) > 0f) continue;

                Vector3 eye = hit.position + Vector3.up * EnemyEyes.Height;
                if (!SightLines.IsClear(eye, chest, Ai.ObstacleMask, Ai.Perception.Target.transform)) continue;

                spot = hit.position;
                return true;
            }
            return false;
        }
    }
}
