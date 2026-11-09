using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;
using CGD.Core;

namespace CGD.Enemies
{
    // Holds its ground and watches where it believes a lost target went, instead of walking
    // into its guns: a marksman covering the corner it slipped behind, a predator lying in
    // wait. It watches the way a person would: along open sightlines toward where the target
    // went, sweeping now and then instead of staring at one spot, and never at a wall in front
    // of its nose. If its spot has no view that way it steps to one that has. Succeeds the
    // moment the target shows itself again (so the tree can react to it) or when time is up;
    // fails without a target.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Overwatch", "Watches along open sightlines toward where the lost target is believed to be, sweeping and shifting for a view. Succeeds when it reappears or time is up.")]
    public sealed class Overwatch : EnemyAction
    {
        // A look direction counts as a view while nothing blocks it this far.
        private const float OpenSightline = 6f;
        // How far either side of the target's direction a glance may stray.
        private const float ScanArc = 70f;
        private const int   LookTries = 6;
        private const int   VantageTries = 8;
        private const float VantageMin = 3f;
        private const float VantageMax = 8f;
        private const float ArrivalDistance = 0.8f;
        private const float VantageSpeedShare = 0.6f;
        private const float EyeHeight = 1.5f;

        [Tooltip("Seconds to keep watching (random within the range)")]
        [SerializeField] private FloatRange _seconds = new(4f, 6f);
        [Tooltip("Seconds between glances in a new direction (random within the range)")]
        [SerializeField] private FloatRange _glanceEvery = new(1.2f, 2.6f);

        [NonSerialized] private float   _endsAt;
        [NonSerialized] private float   _nextGlance;
        [NonSerialized] private Vector3 _lookDirection;
        [NonSerialized] private bool    _shifting;

        public override string Summary => Mathf.Approximately(_seconds.Min, _seconds.Max)
            ? $"{_seconds.Min:0.#} s"
            : $"{_seconds.Min:0.#}–{_seconds.Max:0.#} s";

        protected override void OnEnter(BtContext context)
        {
            _endsAt = context.Time + _seconds.EvaluateClamped();
            if (Ai.Agent.isOnNavMesh) Ai.Agent.ResetPath();

            Vector3 belief = Ai.Perception.TargetPosition;
            _shifting = !HasOpenView(belief) && TryShiftToView(belief);
            PickLook(belief, context.Time);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            var perception = Ai.Perception;
            if (perception.Target == null) return BtStatus.Failure;
            if (perception.IsTargetDetected || context.Time >= _endsAt) return BtStatus.Success;

            Ai.ReportState(EnemyAI.AiState.Hunt);
            Vector3 belief = perception.TargetPosition;

            if (_shifting)
            {
                if (!HasArrived()) return BtStatus.Running;

                _shifting = false;
                Ai.Agent.ResetPath();
                PickLook(belief, context.Time);
            }

            if (context.Time >= _nextGlance) PickLook(belief, context.Time);
            Ai.FaceTowards(Ai.transform.position + _lookDirection * OpenSightline, context.DeltaTime);
            return BtStatus.Running;
        }

        // Whether the way toward where the target went is open for a few metres, rather than a wall.
        private bool HasOpenView(Vector3 belief)
        {
            Vector3 toBelief = FlatDirection(belief);
            return IsOpen(toBelief, Mathf.Min(OpenSightline, Vector3.Distance(Flat(belief), Flat(Ai.transform.position))));
        }

        // Picks a nearby spot with a clear line to the believed position and heads there.
        private bool TryShiftToView(Vector3 belief)
        {
            Vector3 origin = Ai.transform.position;
            for (int i = 0; i < VantageTries; i++)
            {
                Vector2 ring = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(VantageMin, VantageMax);
                Vector3 candidate = origin + new Vector3(ring.x, 0f, ring.y);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;
                if (!SightLines.IsClear(hit.position + Vector3.up * EyeHeight, belief + Vector3.up, Ai.ObstacleMask, null)) continue;

                Ai.Agent.isStopped = false;
                if (Ai.Agent.SetDestination(hit.position)) return true;
            }
            return false;
        }

        private bool HasArrived()
        {
            var agent = Ai.Agent;
            if (agent.pathPending) return false;

            Ai.SetSpeed(Ai.Data.ChaseSpeed * VantageSpeedShare);
            return !agent.hasPath || agent.remainingDistance <= ArrivalDistance;
        }

        // Looks toward the believed position when that way is open; otherwise along the open
        // sightline nearest to it, so it never stares at a wall.
        private void PickLook(Vector3 belief, float now)
        {
            _nextGlance = now + _glanceEvery.EvaluateClamped();

            Vector3 toBelief = FlatDirection(belief);
            for (int i = 0; i < LookTries; i++)
            {
                float   angle     = i == 0 ? 0f : UnityEngine.Random.Range(-ScanArc, ScanArc);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * toBelief;
                if (!IsOpen(direction, OpenSightline)) continue;

                _lookDirection = direction;
                return;
            }

            // Boxed in on that side: face wherever the view runs furthest.
            float best = -1f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                float   clear     = ClearDistance(direction);
                if (clear <= best) continue;

                best = clear;
                _lookDirection = direction;
            }
        }

        private bool IsOpen(Vector3 direction, float distance) => ClearDistance(direction, distance) >= distance;

        private float ClearDistance(Vector3 direction, float max = 12f)
        {
            Vector3 eye = Ai.transform.position + Vector3.up * EyeHeight;
            return Physics.Raycast(eye, direction, out RaycastHit hit, max, Ai.ObstacleMask, QueryTriggerInteraction.Ignore)
                ? hit.distance
                : max;
        }

        private Vector3 FlatDirection(Vector3 point)
        {
            Vector3 flat = Flat(point) - Flat(Ai.transform.position);
            return flat.sqrMagnitude > 0.01f ? flat.normalized : Ai.transform.forward;
        }

        private static Vector3 Flat(Vector3 point) => new(point.x, 0f, point.z);
    }
}
