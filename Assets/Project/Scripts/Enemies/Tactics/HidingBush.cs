using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // A bush to break off into, away from a threat: the one that puts most distance between it
    // and the threat for the least running, not on the threat's side of it, and reachable
    // without a long way round. Only bushes it knows of (BushMemory), each judged as the patch it
    // covers: it plans which side to go in by and where in it to lie (BushApproach), so the bush
    // stands between it and the threat. Shared by Hide In Bush and falling back to regroup.
    internal static class HidingBush
    {
        private const float SnapDistance = 2f;
        private const float PathSlack    = 1.8f;  // a run this many times the straight line is the long way round
        private const float TowardThreat = -0.2f; // bushes further this way toward the threat than this are out
        private const float Chance       = 2f;    // metres of random preference, so it doesn't always pick the same bush

        // The way into the best bush: round to a point outside it first when going in directly
        // would face the threat, then on to where it lies.
        public static bool TryPickRoute(EnemyAI ai, Vector3 threat, float minFromThreat, float maxRun, out BushRoute route)
        {
            route = default;
            Vector3 self = ai.transform.position;
            Vector3 away = Flat(self - threat).normalized;

            float best = float.NegativeInfinity;
            IReadOnlyList<BushMemory.KnownBush> known = ai.Ground.Bushes.Bushes;
            for (int i = 0; i < known.Count; i++)
            {
                if (known[i].Zone == null || known[i].Area.Contains(self)) continue;

                BushRoute plan   = BushApproach.PlanHide(self, threat, known[i].Area);
                Vector3   toBush = Flat(plan.Spot - self);
                float     run    = toBush.magnitude;
                float     fromThreat = Flat(plan.Spot - threat).magnitude;
                if (run > maxRun || fromThreat < minFromThreat) continue;
                if (run > 0.01f && away.sqrMagnitude > 0.01f && Vector3.Dot(toBush / run, away) < TowardThreat) continue;

                float score = fromThreat - run * 0.5f + Random.value * Chance;
                if (score <= best || !TrySnap(ai, self, plan, out BushRoute reachable)) continue;

                best  = score;
                route = reachable;
            }
            return best > float.NegativeInfinity;
        }

        // Just the spot to lie at, for callers that walk straight there.
        public static bool TryPick(EnemyAI ai, Vector3 threat, float minFromThreat, float maxRun, out Vector3 spot)
        {
            bool found = TryPickRoute(ai, threat, minFromThreat, maxRun, out BushRoute route);
            spot = route.Spot;
            return found;
        }

        // The route with its points on the NavMesh, and the spot reachable from there.
        private static bool TrySnap(EnemyAI ai, Vector3 self, BushRoute plan, out BushRoute snapped)
        {
            snapped = default;
            Vector3 via = default;
            if (plan.HasVia)
            {
                if (!NavMesh.SamplePosition(plan.Via, out NavMeshHit viaHit, SnapDistance, NavMesh.AllAreas)) return false;
                via = viaHit.position;
            }

            Vector3 from = plan.HasVia ? via : self;
            if (!ReachableSpot.TryFind(from, plan.Spot, SnapDistance, PathSlack, 2f, ai.Health, out Vector3 spot)) return false;

            snapped = new BushRoute(plan.HasVia, via, spot);
            return true;
        }

        private static Vector3 Flat(Vector3 vector) => new(vector.x, 0f, vector.z);
    }
}
