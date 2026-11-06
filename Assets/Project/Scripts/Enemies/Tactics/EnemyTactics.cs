using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // An enemy's use of the space around it, from the cover it knows (CoverKnowledge) and
    // where it believes its threat is (never the threat's real position unless it sees it):
    //   cover      a known spot whose obstacle stands between it and the threat, near where
    //              it wants to fight from, not taken by an ally — checked with a ray from the
    //              threat before it commits
    //   flank      a spot off to the threat's side, reached by the route the threat is least
    //              likely to see (fewest path corners in its view)
    //   hiding     known cover near a position where someone could be hiding from it — what
    //              a search should check
    // Shared by the state machine and the behavior tree nodes.
    internal sealed class EnemyTactics
    {
        private const float CoverSearchRadius = 14f;
        private const float MinThreatDistance = 4f;
        private const float FacingDot         = 0.35f;
        private const float InCoverDistance   = 1.2f;
        private const float AbandonDistance   = 4f;
        private const int   VerifiedCandidates = 3;
        private const float EyeHeight          = 1.5f;
        private const float FlankDetourLimit   = 3f;

        private static readonly float[] FlankAngles = { 70f, 100f };

        private readonly EnemyAI        _ai;
        private readonly LayerMask      _obstacleMask;
        private readonly CoverKnowledge _knowledge;
        private readonly List<int>      _nearby = new();
        private readonly (int index, float score)[] _best = new (int, float)[VerifiedCandidates];
        private readonly NavMeshPath    _path = new();

        private int _claimed = -1;

        public EnemyTactics(EnemyAI ai, LayerMask obstacleMask)
        {
            _ai           = ai;
            _obstacleMask = obstacleMask;
            _knowledge    = new CoverKnowledge(ai, obstacleMask);
        }

        public bool HasCover => _claimed >= 0 && _knowledge.Map != null;
        public Vector3 CoverPosition => HasCover ? _knowledge.Map[_claimed].Position : _ai.transform.position;

        public void Tick(float time)
        {
            _knowledge.Tick(time);

            // Walked away from its spot (chasing, fleeing): let an ally have it.
            if (HasCover && (CoverPosition - _ai.transform.position).sqrMagnitude > AbandonDistance * AbandonDistance &&
                (_ai.Agent.destination - CoverPosition).sqrMagnitude > 1f)
                ReleaseCover();
        }

        public void Reset()
        {
            ReleaseCover();
            _knowledge.Forget();
        }

        public void ReleaseCover()
        {
            if (_claimed >= 0 && _knowledge.Map != null) _knowledge.Map.Release(_claimed, _ai);
            _claimed = -1;
        }

        // At its spot, and the spot still stands between it and the threat.
        public bool IsInCover(Vector3 threat)
        {
            if (!HasCover) return false;
            CoverPoint cover = _knowledge.Map[_claimed];
            return (cover.Position - _ai.transform.position).sqrMagnitude <= InCoverDistance * InCoverDistance &&
                   cover.Faces(threat, FacingDot);
        }

        // Picks and claims the best known cover against the threat (a different spot from the
        // current one, so this also repositions). False if it knows of nothing suitable.
        public bool TryTakeCover(Vector3 threat, CoverUse use, out Vector3 destination)
        {
            destination = default;
            CoverMap map = _knowledge.Map;
            if (map == null) return false;

            _knowledge.Known(_ai.transform.position, CoverSearchRadius, _nearby);
            for (int i = 0; i < _best.Length; i++) _best[i] = (-1, float.MinValue);

            foreach (int index in _nearby)
            {
                if (index == _claimed || map.IsTakenByOther(index, _ai)) continue;
                if (TryScore(map[index], threat, use, out float score)) Keep(index, score);
            }

            // Only the best few get a ray from the threat: cheap, and it catches cover that
            // faces the right way but is too short or has a gap.
            Vector3 threatEye = threat + Vector3.up * EyeHeight;
            foreach (var (index, _) in _best)
            {
                if (index < 0) continue;
                CoverPoint cover = map[index];
                Vector3 body = cover.Position + Vector3.up * (cover.IsHigh ? 1.3f : 0.6f);
                if (!Physics.Linecast(threatEye, body, _obstacleMask, QueryTriggerInteraction.Ignore)) continue;

                ReleaseCover();
                if (!map.TryClaim(index, _ai)) continue;
                _claimed    = index;
                destination = cover.Position;
                return true;
            }
            return false;
        }

        // A spot at standOff metres off to the threat's side, by the least visible route.
        public bool TryPlanFlank(Vector3 threat, float standOff, out Vector3 destination)
        {
            destination = default;
            Vector3 self = _ai.transform.position;
            Vector3 axis = self - threat;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.01f) return false;
            axis.Normalize();

            float straight  = Vector3.Distance(self, threat);
            float bestScore = float.MinValue;
            int   firstSide = Random.value < 0.5f ? -1 : 1;

            for (int s = 0; s < 2; s++)
            foreach (float angle in FlankAngles)
            {
                int     side      = s == 0 ? firstSide : -firstSide;
                Vector3 candidate = threat + Quaternion.Euler(0f, angle * side, 0f) * axis * standOff;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;
                if (!NavMesh.CalculatePath(self, hit.position, NavMesh.AllAreas, _path) ||
                    _path.status != NavMeshPathStatus.PathComplete) continue;

                float length = PathLength(_path);
                if (length > straight * FlankDetourLimit + standOff) continue;

                float score = -2f * Exposure(_path, threat) - 0.3f * length / Mathf.Max(1f, straight) + Random.value * 0.3f;
                if (score <= bestScore) continue;

                bestScore   = score;
                destination = hit.position;
            }
            return bestScore > float.MinValue;
        }

        // Known spots near a position that are screened from where this enemy stands —
        // where someone hiding from it would be.
        public void HidingSpots(Vector3 around, float radius, List<Vector3> results, int max)
        {
            CoverMap map = _knowledge.Map;
            if (map == null) return;

            _knowledge.Known(around, radius, _nearby);
            Vector3 self = _ai.transform.position;
            foreach (int index in _nearby)
            {
                if (results.Count >= max) return;
                CoverPoint cover = map[index];
                if (cover.Faces(self, FacingDot)) results.Add(cover.Position);
            }
        }

        // --- Scoring ----------------------------------------------------------------

        private bool TryScore(CoverPoint cover, Vector3 threat, CoverUse use, out float score)
        {
            score = 0f;
            if (!cover.Faces(threat, FacingDot)) return false;

            float threatDistance = Vector3.Distance(cover.Position, threat);
            if (threatDistance < MinThreatDistance) return false;

            float travel = Vector3.Distance(cover.Position, _ai.transform.position);
            score = -1.5f * travel / CoverSearchRadius + Random.value * 0.3f;

            if (use == CoverUse.Fight)
            {
                // Somewhere it can still shoot from, near the range it likes to fight at.
                if (threatDistance > _ai.Data.SightRange) return false;
                float preferred = Mathf.Max(1f, _ai.Data.PreferredRange);
                score -= Mathf.Abs(threatDistance - preferred) / preferred;
                if (cover.IsHigh) score -= 0.5f;
            }
            else
            {
                score += threatDistance / 20f + (cover.IsHigh ? 1f : 0f);
            }
            return true;
        }

        private void Keep(int index, float score)
        {
            int worst = 0;
            for (int i = 1; i < _best.Length; i++)
                if (_best[i].score < _best[worst].score) worst = i;
            if (score > _best[worst].score) _best[worst] = (index, score);
        }

        // Share of the route's corners the threat could see.
        private float Exposure(NavMeshPath path, Vector3 threat)
        {
            Vector3[] corners = path.corners;
            if (corners.Length < 2) return 0f;

            Vector3 eye = threat + Vector3.up * EyeHeight;
            int seen = 0;
            for (int i = 1; i < corners.Length; i++)
                if (!Physics.Linecast(eye, corners[i] + Vector3.up * 1.2f, _obstacleMask, QueryTriggerInteraction.Ignore)) seen++;
            return seen / (float)(corners.Length - 1);
        }

        private static float PathLength(NavMeshPath path)
        {
            Vector3[] corners = path.corners;
            float length = 0f;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            return length;
        }
    }
}
