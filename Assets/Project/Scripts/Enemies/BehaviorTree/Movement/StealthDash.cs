using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Stealth;

namespace CGD.Enemies
{
    // A burst of speed a few metres off in a direction its hostiles won't expect, for the moment
    // right after it vanishes (Ambush), so where it was last seen is the wrong place to look.
    // Prefers ground out of every hostile's sight (behind scenery or in a bush), across or
    // against the way it was heading (where a lost target is guessed to have gone), and never
    // towards them; a little chance in the choice keeps it from always taking the same way out.
    // Succeeds once there (or after a moment); fails with nowhere to go.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Stealth Dash", "Bursts off out of sight, away from where hostiles would guess it went. Fails with nowhere to go.")]
    public sealed class StealthDash : EnemyAction
    {
        private const int   Directions   = 12;
        private const float ChestHeight  = 1.1f;
        private const float Unseen       = 3f;    // score for each hostile that can't see the spot
        private const float Unexpected   = 1.5f;  // score for going against its heading
        private const float Retreating   = 1f;    // score for ending further from the hostiles
        private const float Chance       = 0.6f;  // random score, so the way out varies

        [Tooltip("Metres: how far it bursts. Blocked directions may end shorter, down to half this")]
        [SerializeField, Min(1f)] private float _distance = 6f;
        [Tooltip("Speed while dashing, as a multiple of chase speed")]
        [SerializeField, Min(1f)] private float _speed = 2.8f;
        [SerializeField, Min(0.1f)] private float _maxTime = 1.2f;

        [NonSerialized] private bool  _moving;
        [NonSerialized] private float _endsAt;

        public override string Summary => $"{_distance:0.#} m";

        // SetSpeed scales acceleration with the burst: off the mark at once and round corners
        // without drifting wide — a dash, not a jog.
        protected override void OnEnter(BtContext context)
        {
            _endsAt = context.Time + _maxTime;
            _moving = TryPickSpot(out Vector3 destination) && Ai.Routing.SetDestination(destination);
            if (_moving) Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_moving) return BtStatus.Failure;

            Ai.ReportStep(EngageStep.Dash);
            Ai.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            return context.Time >= _endsAt || HasArrived(Ai.Agent, 0.5f) ? BtStatus.Success : BtStatus.Running;
        }

        private bool TryPickSpot(out Vector3 destination)
        {
            destination = default;
            Vector3 from     = Ai.transform.position;
            // Below a walk it was as good as standing: no heading to be guessed from.
            Vector3 velocity = Flat(Ai.Agent.velocity);
            Vector3 heading  = velocity.sqrMagnitude > 0.25f ? velocity.normalized : Vector3.zero;
            IReadOnlyList<HealthManager> hostiles = Ai.Perception.HostilesAround;
            Vector3 toThreat = ThreatDirection(from, hostiles);

            float best   = float.NegativeInfinity;
            float offset = UnityEngine.Random.value * 360f;
            for (int i = 0; i < Directions; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, offset + i * 360f / Directions, 0f) * Vector3.forward;
                // Never towards them: that is where it would be bumped into, and seen up close.
                if (toThreat != Vector3.zero && Vector3.Dot(direction, toThreat) > 0.35f) continue;
                if (!TryReach(from, direction, out Vector3 spot)) continue;

                float score = Score(spot, direction, heading, from, hostiles);
                if (score <= best) continue;

                best        = score;
                destination = spot;
            }
            return best > float.NegativeInfinity;
        }

        // As far along the direction as the navmesh goes, if that is far enough to be worth it.
        private bool TryReach(Vector3 from, Vector3 direction, out Vector3 spot)
        {
            Vector3 to = from + direction * _distance;
            if (NavMesh.Raycast(from, to, out NavMeshHit blocked, NavMesh.AllAreas))
            {
                spot = blocked.position;
                return Flat(spot - from).magnitude >= _distance * 0.5f;
            }
            if (NavMesh.SamplePosition(to, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                spot = hit.position;
                return true;
            }
            spot = default;
            return false;
        }

        private float Score(Vector3 spot, Vector3 direction, Vector3 heading, Vector3 from, IReadOnlyList<HealthManager> hostiles)
        {
            float score = UnityEngine.Random.value * Chance;

            // A lost target is guessed to have carried on the way it was going (BeliefModel), so
            // across or back the other way is where it is least looked for.
            if (heading != Vector3.zero) score += Unexpected * (1f - Vector3.Dot(direction, heading)) * 0.5f;

            for (int i = 0; i < hostiles.Count; i++)
            {
                HealthManager hostile = hostiles[i];
                if (hostile == null || hostile.IsDead) continue;

                Vector3 eye = hostile.transform.position + Vector3.up * EnemyEyes.Height;
                if (!CanSee(eye, spot + Vector3.up * ChestHeight, hostile.transform)) score += Unseen;

                float before = Flat(from - hostile.transform.position).magnitude;
                float after  = Flat(spot - hostile.transform.position).magnitude;
                score += Retreating * Mathf.Clamp((after - before) / _distance, -1f, 1f);
            }
            return score;
        }

        private bool CanSee(Vector3 eye, Vector3 spot, Transform watcher) =>
            !ConcealmentZone.BlocksLine(eye, spot) && SightLines.IsClear(eye, spot, Ai.ObstacleMask, watcher);

        // Towards the nearest hostile around, or where fire last came from; zero with neither.
        private Vector3 ThreatDirection(Vector3 from, IReadOnlyList<HealthManager> hostiles)
        {
            for (int i = 0; i < hostiles.Count; i++)
            {
                if (hostiles[i] == null || hostiles[i].IsDead) continue;
                Vector3 toHostile = Flat(hostiles[i].transform.position - from);
                if (toHostile != Vector3.zero) return toHostile.normalized;
            }
            return Ai.Perception.ThreatDirection;
        }

        private static Vector3 Flat(Vector3 vector)
        {
            vector.y = 0f;
            return vector.sqrMagnitude > 0.0001f ? vector : Vector3.zero;
        }
    }
}
