using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Core;
using CGD.Stealth;

namespace CGD.Enemies
{
    // Stalks its target while it lies low (cloaked, recovering) instead of standing still:
    // circles it a few metres at a time at a wary distance, from one spot out of every
    // hostile's sight to the next, stopping at each to watch. Its eyes stay on the target
    // throughout, so what it knows (where the target is, which way it is heading) is fresh
    // when it comes back in, and from a side it wasn't last seen on. Succeeds when time is up;
    // fails without a target.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Prowl", "Circles the target at a distance, unseen spot to unseen spot, watching it. Succeeds when time is up.")]
    public sealed class Prowl : EnemyAction
    {
        private const int   Candidates      = 10;
        private const float MinSweep        = 15f;   // degrees round the target per move
        private const float MaxSweep        = 55f;
        private const float RadiusJitter    = 1.5f;
        private const float SnapDistance    = 1.5f;
        private const float PathSlack       = 2f;    // a walk this many times the straight line isn't a step round
        private const float ArrivalDistance = 0.6f;
        private const float MoveGiveUp      = 3f;
        private const float ChestHeight     = 1.1f;
        private const float Unseen          = 3f;    // score for each hostile that can't see the spot
        private const float KeepCircling    = 1f;    // score for carrying on round the same way
        private const float Chance          = 0.5f;  // random score, so the circuit varies
        private const int   MaxCorners      = 16;

        [Tooltip("Seconds to prowl (random within the range)")]
        [SerializeField] private FloatRange _seconds = new(3f, 4.5f);
        [Tooltip("Metres it keeps from the target: closer, it backs off; further, it closes in")]
        [SerializeField] private FloatRange _distance = new(7f, 11f);
        [Tooltip("Seconds it stops to watch at each spot (random within the range)")]
        [SerializeField] private FloatRange _pause = new(0.4f, 0.9f);
        [Tooltip("Speed while prowling, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1f)] private float _speed = 0.55f;

        [NonSerialized] private float _endsAt;
        [NonSerialized] private float _pauseUntil;
        [NonSerialized] private float _moveGivesUpAt;
        [NonSerialized] private bool  _moving;
        [NonSerialized] private int   _sense;   // which way round it circles: +1 or -1
        [NonSerialized] private NavMeshPath _path;
        [NonSerialized] private Vector3[]   _corners;

        public override string Summary => $"{_distance.Min:0.#}–{_distance.Max:0.#} m";

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            _path    = new NavMeshPath();
            _corners = new Vector3[MaxCorners];
        }

        protected override void OnEnter(BtContext context)
        {
            _endsAt     = context.Time + _seconds.EvaluateClamped();
            _sense      = UnityEngine.Random.value < 0.5f ? 1 : -1;
            _pauseUntil = context.Time;
            _moving     = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            var perception = Ai.Perception;
            if (perception.Target == null) return BtStatus.Failure;
            if (context.Time >= _endsAt) return BtStatus.Success;

            Ai.ReportState(perception.IsTargetDetected ? EnemyAI.AiState.Chase : EnemyAI.AiState.Hunt);
            Ai.Motion.KeepEyesOnTarget(context.DeltaTime);

            if (_moving)
            {
                Ai.ReportStep(EngageStep.Circle);
                Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
                if (!HasArrived(Ai.Agent, ArrivalDistance) && context.Time < _moveGivesUpAt) return BtStatus.Running;

                // There: stop and watch a moment before the next move.
                _moving     = false;
                _pauseUntil = context.Time + _pause.EvaluateClamped();
                Ai.Routing.Stop();
                return BtStatus.Running;
            }

            if (context.Time < _pauseUntil) return BtStatus.Running;

            _moving = TryMove(perception.TargetPosition, context.Time);
            if (!_moving) _pauseUntil = context.Time + _pause.EvaluateClamped();   // boxed in: watch, then try again
            return BtStatus.Running;
        }

        // A spot a little further round the target, at the distance it keeps, that hostiles can't
        // see and it can walk to without a long way round.
        private bool TryMove(Vector3 target, float now)
        {
            Vector3 self   = Ai.transform.position;
            Vector3 offset = Flat(self - target);
            if (offset.sqrMagnitude < 0.01f) offset = -Flat(Ai.transform.forward);
            float radius = Mathf.Clamp(offset.magnitude, _distance.Min, _distance.Max);
            Vector3 bearing = offset.normalized;

            float   best      = float.NegativeInfinity;
            Vector3 bestSpot  = default;
            int     bestSense = _sense;
            for (int i = 0; i < Candidates; i++)
            {
                // Mostly carries on round the way it was going; now and then doubles back.
                int     sense = i < Candidates * 0.7f ? _sense : -_sense;
                float   sweep = UnityEngine.Random.Range(MinSweep, MaxSweep) * sense;
                float   reach = Mathf.Clamp(radius + UnityEngine.Random.Range(-RadiusJitter, RadiusJitter), _distance.Min, _distance.Max);
                Vector3 spot  = target + Quaternion.Euler(0f, sweep, 0f) * bearing * reach;
                if (!TryReach(self, spot, out spot)) continue;

                float score = UnityEngine.Random.value * Chance + (sense == _sense ? KeepCircling : 0f) + HiddenScore(spot);
                if (score <= best) continue;

                best      = score;
                bestSpot  = spot;
                bestSense = sense;
            }

            if (best == float.NegativeInfinity || !Ai.Routing.SetDestination(bestSpot)) return false;

            _sense         = bestSense;
            _moveGivesUpAt = now + MoveGiveUp;
            Ai.Agent.isStopped = false;
            return true;
        }

        // On the NavMesh, clear of hazards, and a short walk rather than the long way round.
        private bool TryReach(Vector3 from, Vector3 spot, out Vector3 reached)
        {
            reached = default;
            if (!NavMesh.SamplePosition(spot, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas)) return false;
            if (Hazards.DamagePerSecondAt(hit.position, Ai.Health) > 0f) return false;
            if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, _path) ||
                _path.status != NavMeshPathStatus.PathComplete) return false;

            if (PathLength() > Flat(hit.position - from).magnitude * PathSlack + 1f) return false;
            reached = hit.position;
            return true;
        }

        private float PathLength()
        {
            int count = _path.GetCornersNonAlloc(_corners);
            float length = 0f;
            for (int i = 1; i < count; i++) length += Vector3.Distance(_corners[i - 1], _corners[i]);
            return length;
        }

        private float HiddenScore(Vector3 spot)
        {
            float score = 0f;
            var hostiles = Ai.Perception.HostilesAround;
            for (int i = 0; i < hostiles.Count; i++)
            {
                HealthManager hostile = hostiles[i];
                if (hostile == null || hostile.IsDead) continue;

                Vector3 eye = hostile.transform.position + Vector3.up * EnemyEyes.Height;
                if (!CanSee(eye, spot + Vector3.up * ChestHeight, hostile.transform)) score += Unseen;
            }
            return score;
        }

        private bool CanSee(Vector3 eye, Vector3 spot, Transform watcher) =>
            !ConcealmentZone.BlocksLine(eye, spot) && SightLines.IsClear(eye, spot, Ai.ObstacleMask, watcher);

        private static Vector3 Flat(Vector3 vector) => new(vector.x, 0f, vector.z);
    }
}
