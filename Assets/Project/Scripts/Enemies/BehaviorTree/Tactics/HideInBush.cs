using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Core;
using CGD.Stealth;

namespace CGD.Enemies
{
    // Breaks off and goes to ground in a bush: runs (straight, not through other bushes) to one
    // a good way from its target and not past it, then lies still inside, eyes on where the
    // target is, for a while. A bush hides it from sight, so it comes out of this knowing where
    // the target is while the target has lost it — the moment to start stalking again.
    // Succeeds once it has lain low for the hold time; fails when there is no such bush within
    // reach or it can't get there in time (put Take Cover or Flee after it).
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Hide In Bush", "Runs to a bush away from the target and lies low in it, watching. Fails when no bush is in reach.")]
    public sealed class HideInBush : EnemyAction
    {
        private const float ArrivalDistance = 0.8f;
        private const float SnapDistance    = 2f;
        private const float PathSlack       = 1.8f;  // a run this many times the straight line is the long way round
        private const float TowardTarget    = -0.2f; // bushes further this way toward the target than this are out
        private const float Chance          = 2f;    // metres of random preference, so it doesn't always pick the same bush
        private const int   MaxCorners      = 16;

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
        [NonSerialized] private NavMeshPath _path;
        [NonSerialized] private Vector3[]   _corners;

        public override string Summary => $"{_minFromTarget:0}+ m from target, {_hold.Min:0.#}–{_hold.Max:0.#} s";

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            _path    = new NavMeshPath();
            _corners = new Vector3[MaxCorners];
        }

        protected override void OnEnter(BtContext context)
        {
            _holding = false;
            _until   = context.Time + _giveUpAfter;
            if (!TryPickBush(out Vector3 bush) || !Ai.Routing.SetDestination(bush))
            {
                _until = float.NegativeInfinity;   // nowhere to go: fail on the first tick
                return;
            }
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

        // The bush that puts most distance between it and the target for the least running,
        // not on the target's side of it, reachable without a long way round.
        private bool TryPickBush(out Vector3 bush)
        {
            bush = default;
            Vector3 self   = Ai.transform.position;
            Vector3 target = Ai.Perception.TargetPosition;
            Vector3 away   = Flat(self - target).normalized;

            float best = float.NegativeInfinity;
            IReadOnlyList<ConcealmentZone> zones = ConcealmentZone.Active;
            for (int i = 0; i < zones.Count; i++)
            {
                if (!BushHops.Hides(zones[i]) || zones[i].Contains(self)) continue;

                Vector3 center  = zones[i].GroundCenter;
                Vector3 toBush  = Flat(center - self);
                float   run     = toBush.magnitude;
                float   fromTarget = Flat(center - target).magnitude;
                if (run > _maxRun || fromTarget < _minFromTarget) continue;
                if (run > 0.01f && away.sqrMagnitude > 0.01f && Vector3.Dot(toBush / run, away) < TowardTarget) continue;

                float score = fromTarget - run * 0.5f + UnityEngine.Random.value * Chance;
                if (score <= best || !TryReach(self, center, run, out Vector3 spot)) continue;

                best = score;
                bush = spot;
            }
            return best > float.NegativeInfinity;
        }

        private bool TryReach(Vector3 from, Vector3 center, float straight, out Vector3 spot)
        {
            spot = default;
            if (!NavMesh.SamplePosition(center, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas)) return false;
            if (Hazards.DamagePerSecondAt(hit.position, Ai.Health) > 0f) return false;
            if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, _path) ||
                _path.status != NavMeshPathStatus.PathComplete) return false;

            int count = _path.GetCornersNonAlloc(_corners);
            float length = 0f;
            for (int i = 1; i < count; i++) length += Vector3.Distance(_corners[i - 1], _corners[i]);
            if (length > straight * PathSlack + 2f) return false;

            spot = hit.position;
            return true;
        }

        private static Vector3 Flat(Vector3 vector) => new(vector.x, 0f, vector.z);
    }
}
