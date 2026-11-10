using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;

namespace CGD.Enemies
{
    // Plans where an enemy withdraws to when a fight turns against it, from what it knows rather
    // than from panic: the cover it has learned (CoverKnowledge), where every hostile it senses
    // stands now, where the one it fights is believed to be, and its post (home ground, where help
    // and familiar cover are). A good fallback is cover that screens it from the threat, well away
    // from every hostile (up to the range it likes to fight at, no further), reached by a route
    // that neither runs past a hostile nor stays in the threat's view, and not far from home.
    // The plan is kept for a moment and made again once the situation has moved on.
    internal sealed class FallbackPlanner
    {
        private const float SearchRadius   = 28f;
        private const float FacingDot      = 0.35f;
        private const float RefreshAfter   = 2.5f;
        private const float ThreatDrift    = 6f;     // metres the threat may move before the plan is stale
        private const float MinSafeDistance = 8f;    // no hostile nearer than this to the spot
        private const float MinGain        = 3f;     // and it must put at least this much more room between them
        private const float MinRouteClearance = 4f;  // the route never passes closer than this to a hostile
        private const int   RouteChecks    = 4;

        private readonly EnemyAI        _ai;
        private readonly CoverKnowledge _knowledge;
        private readonly LayerMask      _obstacleMask;
        private readonly List<int>      _nearby = new();
        private readonly List<(int index, float score)> _scored = new();
        private readonly NavMeshPath    _path = new();

        private int     _index = -1;
        private Vector3 _threatAtPlan;
        private float   _plannedAt = float.NegativeInfinity;

        public FallbackPlanner(EnemyAI ai, CoverKnowledge knowledge, LayerMask obstacleMask)
        {
            _ai           = ai;
            _knowledge    = knowledge;
            _obstacleMask = obstacleMask;
        }

        // The planned spot's index in the CoverMap, −1 for none.
        public int Planned => _index;

        public void Clear() => _index = -1;

        // Whether the plan made earlier still fits: recent, the threat hasn't moved far, and no
        // hostile has got close to the spot meanwhile.
        public bool StillHolds(Vector3 threat, IReadOnlyList<HealthManager> hostiles)
        {
            CoverMap map = _knowledge.Map;
            if (_index < 0 || map == null || _index >= map.Count) return false;
            if (Time.time - _plannedAt > RefreshAfter) return false;
            if ((threat - _threatAtPlan).sqrMagnitude > ThreatDrift * ThreatDrift) return false;
            return NearestDistance(map[_index].Position, hostiles, threat) >= MinSafeDistance;
        }

        // Picks the best fallback spot (not claimed yet). False when it knows of none that is
        // safe, screened and reachable without passing a hostile.
        public bool TryPlan(Vector3 threat, IReadOnlyList<HealthManager> hostiles, System.Func<int, bool> isTaken, out int index)
        {
            index = -1;
            CoverMap map = _knowledge.Map;
            if (map == null) return false;

            Vector3 self = _ai.transform.position;
            float roomNow = NearestDistance(self, hostiles, threat);
            float preferred = Mathf.Max(1f, _ai.Data.PreferredRange);

            _knowledge.Known(self, SearchRadius, _nearby);
            _scored.Clear();
            foreach (int candidate in _nearby)
            {
                if (isTaken(candidate)) continue;

                CoverPoint cover = map[candidate];
                if (!cover.Faces(threat, FacingDot)) continue;

                float room = NearestDistance(cover.Position, hostiles, threat);
                if (room < MinSafeDistance || room < roomNow + MinGain) continue;

                float travel = Vector3.Distance(cover.Position, self);
                float home   = Mathf.Min(1f, Vector3.Distance(cover.Position, _ai.Post.Position) / SearchRadius);
                float score  = Mathf.Min(room, preferred * 1.3f) / preferred
                               - 0.8f * travel / SearchRadius
                               - 0.5f * home
                               + (cover.IsHigh ? 0.3f : 0f)
                               + Random.value * 0.2f;
                _scored.Add((candidate, score));
            }
            _scored.Sort((a, b) => b.score.CompareTo(a.score));

            float bestScore = float.MinValue;
            Vector3 threatEye = threat + Vector3.up * EnemyEyes.Height;
            for (int i = 0; i < _scored.Count && i < RouteChecks; i++)
            {
                CoverPoint cover = map[_scored[i].index];

                // The obstacle really does screen the spot from the threat.
                Vector3 body = cover.Position + Vector3.up * (cover.IsHigh ? 1.3f : 0.6f);
                if (!Physics.Linecast(threatEye, body, _obstacleMask, QueryTriggerInteraction.Ignore)) continue;

                if (!NavMesh.CalculatePath(self, cover.Position, NavMesh.AllAreas, _path) ||
                    _path.status != NavMeshPathStatus.PathComplete) continue;
                if (RouteMetrics.Clearance(_path, hostiles) < MinRouteClearance) continue;

                float score = _scored[i].score - 1.5f * RouteMetrics.Exposure(_path, threatEye, _obstacleMask);
                if (score <= bestScore) continue;

                bestScore = score;
                index     = _scored[i].index;
            }

            if (index < 0) return false;
            _threatAtPlan = threat;
            _plannedAt    = Time.time;
            _index        = index;
            return true;
        }

        // The nearest hostile to a spot; the believed threat counts even when nothing is sensed.
        private static float NearestDistance(Vector3 spot, IReadOnlyList<HealthManager> hostiles, Vector3 threat)
        {
            float nearest = Vector3.Distance(spot, threat);
            for (int i = 0; i < hostiles.Count; i++)
                if (hostiles[i] != null && !hostiles[i].IsDead)
                    nearest = Mathf.Min(nearest, Vector3.Distance(spot, hostiles[i].transform.position));
            return nearest;
        }
    }
}
