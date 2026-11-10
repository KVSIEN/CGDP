using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;

namespace CGD.Enemies
{
    // An enemy's use of the space around it, from the cover it knows (CoverKnowledge) and
    // where it believes its threat is (never the threat's real position unless it sees it):
    //   cover      a known spot whose obstacle stands between it and the threat — near where
    //              it wants to fight from, out of sight, or further in (CoverUse) — not taken
    //              by an ally, checked with a ray from the threat before it commits
    //   flank      a spot off to the threat's side — away from where it was last seen facing —
    //              reached by the route the threat is least likely to see (fewest path
    //              corners in its view)
    //   hiding     known cover near a position where someone could be hiding from it — what
    //              a search should check
    //   unseen     known full-height cover nobody around can see right now, for vanishing
    //              to (Phase Shift)
    //   fallback   a planned withdrawal (FallbackPlanner): screened cover far from every hostile
    //              it knows of, by a route that doesn't pass them
    // Asked by the behavior tree nodes (Take Cover, Flank, Chase Target, the search).
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
        private const float AdvanceMinGain     = 3f;
        private const float WatchedSidePenalty = 1.5f;
        // Phase Shift: reappear at least this far away, checking at most this many spots.
        private const float UnseenMinJump = 5f;
        private const int   UnseenChecks  = 12;

        private static readonly float[] FlankAngles = { 70f, 100f };

        // Melee turns: strikes before it steps back to let a waiting ally in, and how long it waits.
        private const int   StrikesPerTurn = 2;
        private const float TurnRest       = 0.8f;
        private const float CircleStep     = 35f;    // degrees round the target per new circling point
        private const float CircleRepath   = 0.4f;
        private static readonly Vector2 CircleFlipEvery = new(2.5f, 5f);

        private readonly EnemyAI        _ai;
        private readonly LayerMask      _obstacleMask;
        private readonly CoverKnowledge _knowledge;
        private readonly List<int>      _nearby = new();
        private readonly (int index, float score)[] _best = new (int, float)[VerifiedCandidates];
        private readonly NavMeshPath    _path = new();
        private readonly FallbackPlanner _fallback;
        private readonly List<(int index, float score)> _unseen = new();

        private int   _claimed = -1;
        private bool  _hasMeleeTurn;
        private int   _strikes;
        private float _turnRestUntil;
        private int   _circleDirection = 1;
        private float _nextCircleFlip;
        private float _nextCirclePath;

        public EnemyTactics(EnemyAI ai, LayerMask obstacleMask)
        {
            _ai           = ai;
            _obstacleMask = obstacleMask;
            _knowledge    = new CoverKnowledge(ai, obstacleMask);
            _fallback     = new FallbackPlanner(ai, _knowledge, obstacleMask);
        }

        // Its latest cover, flank and vanish searches, for the spectator's overlays.
        public TacticsLog Log { get; } = new();

        public bool HasCover => _claimed >= 0 && _knowledge.Map != null;
        public Vector3 CoverPosition => HasCover ? _knowledge.Map[_claimed].Position : _ai.transform.position;
        // The claimed spot's index in the CoverMap, −1 for none.
        public int ClaimedCover => HasCover ? _claimed : -1;

        public bool KnowsCover(int index) => _knowledge.Knows(index);

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
            EndMeleeTurn();
            _knowledge.Forget();
            _fallback.Clear();
            Log.Clear();
        }

        public void ReleaseCover()
        {
            if (_claimed >= 0 && _knowledge.Map != null) _knowledge.Map.Release(_claimed, _ai);
            _claimed = -1;
        }

        // --- Melee turns ------------------------------------------------------------

        // Whether it may step in and strike: only a few enemies (EnemyData.MaxMeleeAttackers,
        // scaled by the director's pacing) are in melee with one target at a time, and it gives
        // way after a couple of strikes.
        public bool HasMeleeTurn => _hasMeleeTurn;

        public bool TryTakeMeleeTurn(HealthManager target)
        {
            if (_hasMeleeTurn) return true;
            if (Time.time < _turnRestUntil) return false;

            _hasMeleeTurn = AttackTokens.TryAcquire(AttackTokenKind.Melee, target, _ai,
                EnemyPacing.Current.ScaleAttackers(_ai.Data.MaxMeleeAttackers));
            return _hasMeleeTurn;
        }

        public void OnStrike()
        {
            if (!_hasMeleeTurn || ++_strikes < StrikesPerTurn) return;
            EndMeleeTurn();
            _turnRestUntil = Time.time + TurnRest;
        }

        public void EndMeleeTurn()
        {
            if (_hasMeleeTurn) AttackTokens.Release(AttackTokenKind.Melee, _ai);
            _hasMeleeTurn = false;
            _strikes      = 0;
        }

        // Waiting its turn: circles the target at a distance, now and then changing direction.
        public void CircleTarget(Vector3 target, float radius)
        {
            float time = Time.time;
            if (time >= _nextCircleFlip)
            {
                _nextCircleFlip  = time + Random.Range(CircleFlipEvery.x, CircleFlipEvery.y);
                _circleDirection = Random.value < 0.5f ? -1 : 1;
            }
            if (time < _nextCirclePath) return;
            _nextCirclePath = time + CircleRepath;

            Vector3 away = _ai.transform.position - target;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -_ai.transform.forward;

            Vector3 point = target + Quaternion.Euler(0f, CircleStep * _circleDirection, 0f) * away.normalized * radius;
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                _ai.Agent.isStopped = false;
                _ai.Agent.SetDestination(hit.position);
            }
            else
            {
                _circleDirection = -_circleDirection;
            }
        }

        // --- Cover ------------------------------------------------------------------

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
        // `nearestToThreat` rules out spots closer to the threat than that (0 = no limit): a
        // long-range fighter pushing up advances to where it fights best, not into the open.
        public bool TryTakeCover(Vector3 threat, CoverUse use, out Vector3 destination, float nearestToThreat = 0f)
        {
            destination = default;
            CoverMap map = _knowledge.Map;
            if (map == null) return false;

            _knowledge.Known(_ai.transform.position, CoverSearchRadius, _nearby);
            for (int i = 0; i < _best.Length; i++) _best[i] = (-1, float.MinValue);

            foreach (int index in _nearby)
            {
                if (index == _claimed || map.IsTakenByOther(index, _ai)) continue;
                if (nearestToThreat > 0f && (map[index].Position - threat).sqrMagnitude < nearestToThreat * nearestToThreat) continue;
                if (TryScore(map[index], threat, use, out float score)) Keep(index, score);
            }

            // Only the best few get a ray from the threat: cheap, and it catches cover that
            // faces the right way but is too short or has a gap. Best first, so the first that
            // passes is the one to take (Keep fills slots in no particular order).
            SortBestFirst(_best);
            TacticalDecision log = Log.Cover;
            log.Begin(threat);
            Vector3 threatEye = threat + Vector3.up * EyeHeight;
            foreach (var (index, score) in _best)
            {
                if (index < 0) continue;
                CoverPoint cover = map[index];
                Vector3 body = cover.Position + Vector3.up * (cover.IsHigh ? 1.3f : 0.6f);
                if (!Physics.Linecast(threatEye, body, _obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    log.Add(cover.Position, score, CandidateVerdict.Exposed);
                    continue;
                }

                ReleaseCover();
                if (!map.TryClaim(index, _ai))
                {
                    log.Add(cover.Position, score, CandidateVerdict.Taken);
                    continue;
                }
                _claimed    = index;
                destination = cover.Position;
                log.Add(destination, score, CandidateVerdict.Picked);
                log.Pick(destination);
                return true;
            }
            return false;
        }

        // The planned fallback against the threat (planned again when the situation has changed),
        // claimed. False if it knows of no safe, screened spot it can reach without passing a hostile.
        public bool TryFallBack(Vector3 threat, out Vector3 destination)
        {
            destination = default;
            IReadOnlyList<HealthManager> hostiles = _ai.Perception.HostilesAround;

            if (_fallback.StillHolds(threat, hostiles) && _fallback.Planned == _claimed && HasCover)
            {
                destination = CoverPosition;
                return true;
            }

            CoverMap map = _knowledge.Map;
            if (map == null || !_fallback.TryPlan(threat, hostiles, spot => map.IsTakenByOther(spot, _ai), out int planned))
                return false;
            return TryClaimCover(planned, out destination);
        }

        // A spot at standOff metres off to the threat's side, by the least visible route.
        // preferSide: −1 / +1 to go round a given side first (squads split their flankers), 0 = either.
        public bool TryPlanFlank(Vector3 threat, float standOff, out Vector3 destination, int preferSide = 0)
        {
            destination = default;
            Vector3 self = _ai.transform.position;
            Vector3 axis = self - threat;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.01f) return false;
            axis.Normalize();

            float   straight  = Vector3.Distance(self, threat);
            float   bestScore = float.MinValue;
            int     bestEntry = -1;
            TacticalDecision log = Log.Flank;
            log.Begin(threat);
            Vector3 watching  = _ai.Perception.TargetFacing;
            int   firstSide = preferSide != 0 ? preferSide : Random.value < 0.5f ? -1 : 1;

            for (int s = 0; s < 2; s++)
            foreach (float angle in FlankAngles)
            {
                int     side      = s == 0 ? firstSide : -firstSide;
                Vector3 candidate = threat + Quaternion.Euler(0f, angle * side, 0f) * axis * standOff;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    log.Add(candidate, 0f, CandidateVerdict.Unreachable);
                    continue;
                }
                if (!NavMesh.CalculatePath(self, hit.position, NavMesh.AllAreas, _path) ||
                    _path.status != NavMeshPathStatus.PathComplete)
                {
                    log.Add(hit.position, 0f, CandidateVerdict.Unreachable);
                    continue;
                }

                float length = RouteMetrics.Length(_path);
                if (length > straight * FlankDetourLimit + standOff)
                {
                    log.Add(hit.position, 0f, CandidateVerdict.TooFar);
                    continue;
                }

                float exposure = RouteMetrics.Exposure(_path, threat + Vector3.up * EyeHeight, _obstacleMask);
                float score = -2f * exposure - 0.3f * length / Mathf.Max(1f, straight) + Random.value * 0.3f
                              - WatchedSidePenalty * Watched(watching, threat, hit.position);
                int entry = log.Add(hit.position, score, CandidateVerdict.Passed);
                if (score <= bestScore) continue;

                bestScore   = score;
                bestEntry   = entry;
                destination = hit.position;
            }

            if (bestScore <= float.MinValue) return false;
            log.Mark(bestEntry, CandidateVerdict.Picked);
            log.Pick(destination);
            return true;
        }

        // The known full-height cover within maxDistance that none of the watchers can see,
        // preferring spots far from the nearest of them — somewhere to vanish to (claim it with
        // TryClaimCover). Checks the watchers' actual positions: it is meant to come out of
        // everyone's sight.
        public bool TryFindUnseenCover(float maxDistance, IReadOnlyList<HealthManager> watchers, out int index)
        {
            index = -1;
            CoverMap map = _knowledge.Map;
            if (map == null) return false;

            Vector3 self = _ai.transform.position;
            TacticalDecision log = Log.Vanish;
            log.Begin(watchers.Count > 0 ? watchers[0].transform.position : self);
            _knowledge.Known(self, maxDistance, _nearby);
            _unseen.Clear();
            foreach (int candidate in _nearby)
            {
                CoverPoint cover = map[candidate];
                if (!cover.IsHigh || candidate == _claimed || map.IsTakenByOther(candidate, _ai)) continue;
                if ((cover.Position - self).sqrMagnitude < UnseenMinJump * UnseenMinJump) continue;
                _unseen.Add((candidate, DistanceToNearest(cover.Position, watchers) + Random.value * 2f));
            }
            _unseen.Sort((a, b) => b.score.CompareTo(a.score));

            for (int i = 0; i < _unseen.Count && i < UnseenChecks; i++)
            {
                CoverPoint cover = map[_unseen[i].index];
                float      score = _unseen[i].score;
                if (IsSeenByAny(cover.Position, watchers))
                {
                    log.Add(cover.Position, score, CandidateVerdict.Seen);
                    continue;
                }
                // Same walkable area: never onto a crate top or an island it can't leave.
                if (!NavMesh.CalculatePath(self, cover.Position, NavMesh.AllAreas, _path) ||
                    _path.status != NavMeshPathStatus.PathComplete)
                {
                    log.Add(cover.Position, score, CandidateVerdict.Unreachable);
                    continue;
                }

                index = _unseen[i].index;
                log.Add(cover.Position, score, CandidateVerdict.Picked);
                log.Pick(cover.Position);
                return true;
            }
            return false;
        }

        // Swaps whatever cover it holds for this spot. False if an ally took it meanwhile.
        public bool TryClaimCover(int index, out Vector3 position)
        {
            position = default;
            CoverMap map = _knowledge.Map;
            if (map == null || index < 0 || index >= map.Count || map.IsTakenByOther(index, _ai)) return false;

            ReleaseCover();
            if (!map.TryClaim(index, _ai)) return false;
            _claimed = index;
            position = map[index].Position;
            return true;
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

        private static float DistanceToNearest(Vector3 spot, IReadOnlyList<HealthManager> watchers)
        {
            float nearest = float.MaxValue;
            for (int i = 0; i < watchers.Count; i++)
                if (watchers[i] != null && !watchers[i].IsDead)
                    nearest = Mathf.Min(nearest, Vector3.Distance(spot, watchers[i].transform.position));
            return nearest == float.MaxValue ? 0f : nearest;
        }

        private bool IsSeenByAny(Vector3 spot, IReadOnlyList<HealthManager> watchers)
        {
            Vector3 body = spot + Vector3.up * 1.3f;
            for (int i = 0; i < watchers.Count; i++)
            {
                HealthManager watcher = watchers[i];
                if (watcher == null || watcher.IsDead) continue;
                Vector3 eye = watcher.transform.position + Vector3.up * EyeHeight;
                if (SightLines.IsClear(eye, body, _obstacleMask, watcher.transform)) return true;
            }
            return false;
        }

        private bool TryScore(CoverPoint cover, Vector3 threat, CoverUse use, out float score)
        {
            score = 0f;
            if (!cover.Faces(threat, FacingDot)) return false;

            float threatDistance = Vector3.Distance(cover.Position, threat);
            if (threatDistance < MinThreatDistance) return false;

            float travel = Vector3.Distance(cover.Position, _ai.transform.position);
            score = -1.5f * travel / CoverSearchRadius + Random.value * 0.3f;

            switch (use)
            {
                case CoverUse.Fight:
                    // Somewhere it can still shoot from, near the range it likes to fight at.
                    if (threatDistance > _ai.Data.SightRange) return false;
                    float preferred = Mathf.Max(1f, _ai.Data.PreferredRange);
                    score -= Mathf.Abs(threatDistance - preferred) / preferred;
                    if (cover.IsHigh) score -= 0.5f;
                    return true;

                case CoverUse.Hide:
                    // Enemies don't crouch, so only full-height cover takes them out of sight.
                    if (!cover.IsHigh) return false;
                    score += threatDistance / 20f;
                    return true;

                case CoverUse.Advance:
                    // Ground gained toward the threat, worth more than the run there costs.
                    float gained = Vector3.Distance(_ai.transform.position, threat) - threatDistance;
                    if (gained < AdvanceMinGain) return false;
                    score += 2f * gained / CoverSearchRadius + (cover.IsHigh ? 0.5f : 0f);
                    return true;
            }
            return false;
        }

        private void Keep(int index, float score)
        {
            int worst = 0;
            for (int i = 1; i < _best.Length; i++)
                if (_best[i].score < _best[worst].score) worst = i;
            if (score > _best[worst].score) _best[worst] = (index, score);
        }

        // Insertion sort: a handful of entries, and no allocation.
        private static void SortBestFirst((int index, float score)[] entries)
        {
            for (int i = 1; i < entries.Length; i++)
            {
                var entry = entries[i];
                int at = i;
                for (; at > 0 && entries[at - 1].score < entry.score; at--) entries[at] = entries[at - 1];
                entries[at] = entry;
            }
        }

        // How squarely a spot lies in the direction the threat was facing: 1 dead ahead of it,
        // 0 at its side or behind it (or when its facing is unknown).
        private static float Watched(Vector3 facing, Vector3 threat, Vector3 spot)
        {
            Vector3 toSpot = spot - threat;
            toSpot.y = 0f;
            if (facing == Vector3.zero || toSpot.sqrMagnitude < 0.01f) return 0f;
            return Mathf.Max(0f, Vector3.Dot(facing, toSpot.normalized));
        }
    }
}
