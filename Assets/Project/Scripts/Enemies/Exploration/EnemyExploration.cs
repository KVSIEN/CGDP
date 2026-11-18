using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // An enemy finding its own way round. It remembers where it has walked (PatrolCoverage) and,
    // when Patrol asks for somewhere to go, weighs a handful of candidates — the good places it
    // has learned (EnemyGround) and a few random walkable spots — by how long since it saw them,
    // what they are worth to its faction and whether blood was spilt there (PatrolScoring). It
    // only ranges round its post (or nearest waypoint) and, when its faction is held to a
    // territory, inside that. Hurt or seeing an ally fall asks Patrol to choose again.
    internal sealed class EnemyExploration
    {
        private const int   RandomCandidates = 6;
        private const float MinHop           = 4f;
        private const float SnapReach        = 2f;

        private readonly EnemyAI                 _ai;
        private readonly PatrolCoverage          _coverage = new();
        private readonly List<GroundPlace>       _places   = new();
        private bool _replan;

        public EnemyExploration(EnemyAI ai) => _ai = ai;

        internal PatrolCoverage Coverage => _coverage;
        private PatrolDoctrine Doctrine => _ai.Doctrine.Patrol;

        public bool ExploresWithoutRoute => Doctrine.ExploreWithoutRoute;

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
            _ai.Ground.CopyPlaces(_places);

            float best = float.NegativeInfinity;
            foreach (GroundPlace place in _places)
            {
                if (place.Traits == PlaceTraits.None) continue;
                Consider(place.Position, PatrolScoring.AppealOf(place, ground), self, anchor, time, patrol, ground, ref best, ref stop);
            }
            for (int i = 0; i < RandomCandidates; i++)
            {
                Vector2 offset = Random.insideUnitCircle * patrol.Range;
                Vector3 spot = anchor + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(spot, out NavMeshHit hit, SnapReach, NavMesh.AllAreas)) continue;
                Consider(hit.position, 0f, self, anchor, time, patrol, ground, ref best, ref stop);
            }
            return best > float.NegativeInfinity;
        }

        private void Consider(Vector3 point, float appeal, Vector3 self, Vector3 anchor, float time, PatrolDoctrine patrol,
                              GroundDoctrine ground, ref float best, ref Vector3 stop)
        {
            if (Flat(point - self).sqrMagnitude < MinHop * MinHop) return;
            float fromAnchor = Flat(point - anchor).magnitude;
            if (fromAnchor > patrol.Range || !_ai.IsWithinHome(point, 0f)) return;

            float score = PatrolScoring.Score(_coverage.Staleness(point, time, patrol.ForgetAfter), appeal,
                                              PatrolScoring.DangerNear(point, _places), fromAnchor / patrol.Range, patrol, ground);
            // A little noise, so two with the same experience do not walk the same tour.
            score += Random.value * 0.05f;
            if (score <= best) return;
            best = score;
            stop = point;
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
