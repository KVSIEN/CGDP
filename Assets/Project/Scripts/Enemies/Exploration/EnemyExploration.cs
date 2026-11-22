using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // An enemy finding its own way round. It remembers where it has walked (PatrolCoverage) and,
    // when Patrol asks for somewhere to go, weighs a handful of candidates — the good places it
    // has learned (EnemyGround) and a few random walkable spots — by how long since it saw them,
    // what they are worth to its faction and whether blood was spilt there (PatrolScoring), and
    // by its faction's taste in where to look (ExploreStyle): TECH works outward from its post a
    // little at a time, its range growing with what it has covered; VOID keeps to the edges of
    // its range and the walls; BIO keeps to the open middle and the concealment it knows. It
    // only ranges round its post (or nearest waypoint) and, when its faction is held to a
    // territory, inside that. Hurt or seeing an ally fall asks Patrol to choose again.
    internal sealed class EnemyExploration
    {
        private const int   RandomCandidates = 8;
        private const float MinHop           = 4f;
        private const float SnapReach        = 2f;
        // An expanding explorer looks for every other candidate this close to itself.
        private const float ExpandHop        = 9f;
        // The share of its range where the outskirts start.
        private const float OutskirtsFrom    = 0.55f;
        // Metres round concealment it knows that an inside explorer also considers.
        private const float AroundCover      = 3f;

        private readonly EnemyAI                 _ai;
        private readonly PatrolCoverage          _coverage = new();
        private readonly List<GroundPlace>       _places   = new();
        private bool _replan;

        public EnemyExploration(EnemyAI ai) => _ai = ai;

        internal PatrolCoverage Coverage => _coverage;
        private PatrolDoctrine Doctrine => _ai.Doctrine.Patrol;

        public bool ExploresWithoutRoute => Doctrine.ExploreWithoutRoute;

        // How far from its post it ranges right now: its full range, or for an expanding faction
        // as far as the ground it has covered so far has taken it.
        public float CurrentRange => Doctrine.Style == ExploreStyle.Expand
            ? PatrolScoring.ExpandedRange(Doctrine.Range, _coverage.Count)
            : Doctrine.Range;

        // On a route: whether this waypoint is where it steps off for somewhere of its own choosing.
        public bool WantsDetour() => Random.value < Doctrine.RouteDrift;

        // True when something happened (hurt, an ally fell) that makes the stop it is heading for worth a second thought.
        public bool NeedsReplan => _replan;

        public void Invalidate() => _replan = true;

        // It has chosen again, so whatever made it think twice is dealt with.
        public void Settle() => _replan = false;

        public void Visit(Vector3 position, float time) => _coverage.Visit(position, time);

        public void Forget()
        {
            _coverage.Forget();
            _replan = false;
        }

        // The best stop it can think of from where it stands; false when nowhere is worth going.
        public bool TryPickStop(float time, out Vector3 stop)
        {
            _replan = false;
            stop = default;

            PatrolDoctrine patrol = Doctrine;
            GroundDoctrine ground = _ai.Doctrine.Ground;
            Vector3 self   = _ai.transform.position;
            Vector3 anchor = _ai.Post.ReturnPoint();
            float   range  = CurrentRange;
            _ai.Ground.CopyPlaces(_places);

            float best = float.NegativeInfinity;
            foreach (GroundPlace place in _places)
            {
                if (place.Traits == PlaceTraits.None) continue;
                Consider(place.Position, PatrolScoring.AppealOf(place, ground), self, anchor, range, time, patrol, ground, ref best, ref stop);
            }
            for (int i = 0; i < RandomCandidates; i++)
            {
                if (!TryCandidate(patrol.Style, i, self, anchor, range, out Vector3 spot)) continue;
                Consider(spot, 0f, self, anchor, range, time, patrol, ground, ref best, ref stop);
            }
            return best > float.NegativeInfinity;
        }

        // A random walkable spot of the kind its faction looks at: anywhere in range; for an
        // expanding explorer every other one close to itself; for the outskirts, out towards the
        // edge of its range; for the inside, every other one round concealment it knows.
        private bool TryCandidate(ExploreStyle style, int index, Vector3 self, Vector3 anchor, float range, out Vector3 spot)
        {
            Vector3 centre = anchor;
            float   radius = range;
            float   from   = 0f;
            bool    alternate = index % 2 == 1;
            if (style == ExploreStyle.Expand && alternate)
            {
                centre = self;
                radius = Mathf.Min(range, ExpandHop);
            }
            else if (style == ExploreStyle.Outskirts)
            {
                from = OutskirtsFrom;
            }
            else if (style == ExploreStyle.Inside && alternate && TryKnownCover(index, out Vector3 cover))
            {
                centre = cover;
                radius = AroundCover;
            }

            Vector2 direction = Random.insideUnitCircle.normalized;
            float   distance  = radius * Mathf.Sqrt(Random.Range(from * from, 1f));   // even over the ring's area
            Vector3 raw = centre + new Vector3(direction.x, 0f, direction.y) * distance;
            spot = default;
            if (!NavMesh.SamplePosition(raw, out NavMeshHit hit, SnapReach, NavMesh.AllAreas)) return false;
            spot = hit.position;
            return true;
        }

        // One of the concealed places it knows, taken in turn by the candidate's index.
        private bool TryKnownCover(int index, out Vector3 cover)
        {
            cover = default;
            int hidden = 0;
            foreach (GroundPlace place in _places)
                if ((place.Traits & PlaceTraits.Hidden) != 0) hidden++;
            if (hidden == 0) return false;

            int pick = index % hidden;
            foreach (GroundPlace place in _places)
            {
                if ((place.Traits & PlaceTraits.Hidden) == 0) continue;
                if (pick-- > 0) continue;
                cover = place.Position;
                return true;
            }
            return false;
        }

        private void Consider(Vector3 point, float appeal, Vector3 self, Vector3 anchor, float range, float time, PatrolDoctrine patrol,
                              GroundDoctrine ground, ref float best, ref Vector3 stop)
        {
            if (Flat(point - self).sqrMagnitude < MinHop * MinHop) return;
            float fromAnchor = Flat(point - anchor).magnitude;
            if (fromAnchor > range || !_ai.IsWithinHome(point, 0f)) return;

            float anchorShare = fromAnchor / range;
            float score = PatrolScoring.Score(_coverage.Staleness(point, time, patrol.ForgetAfter), appeal,
                                              PatrolScoring.DangerNear(point, _places), anchorShare, patrol, ground);
            if (patrol.Style != ExploreStyle.Roam)
                score += PatrolScoring.StyleBonus(patrol.Style, anchorShare, Flat(point - self).magnitude / range,
                                                  WallDistance(point), CoverDistance(point));
            // A little noise, so two with the same experience do not walk the same tour.
            score += Random.value * 0.05f;
            if (score <= best) return;
            best = score;
            stop = point;
        }

        // Metres to the nearest wall (the NavMesh's edge).
        private static float WallDistance(Vector3 point) =>
            NavMesh.FindClosestEdge(point, out NavMeshHit edge, NavMesh.AllAreas) ? edge.distance : float.PositiveInfinity;

        // Metres to the nearest concealment it knows of.
        private float CoverDistance(Vector3 point)
        {
            float nearest = float.PositiveInfinity;
            foreach (GroundPlace place in _places)
                if ((place.Traits & PlaceTraits.Hidden) != 0) nearest = Mathf.Min(nearest, Flat(place.Position - point).magnitude);
            return nearest;
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
