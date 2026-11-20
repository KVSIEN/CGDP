using System.Collections.Generic;
using UnityEngine;
using CGD.Stealth;

namespace CGD.Enemies
{
    // Picks the next bush to pass through on the way somewhere, so a sneaking enemy travels
    // from cover to cover instead of across the open: the nearest bush that brings it closer to
    // where it is going without lengthening the walk by more than its allowance. Asked again at
    // each bush, so a route is a chain of such hops ending at the destination. A hop is a point
    // of the bush's footprint (BushApproach.PassPoint), so a bush on the line is crossed, not
    // detoured to its middle. Flat distances only — the NavMesh decides the actual path between hops.
    public static class BushHops
    {
        // Routes shorter than this go straight: not worth a detour.
        public const float MinRoute = 5f;
        // A bush this close is the one it is in (or as good as).
        public const float MinHop = 2f;
        // Each hop has to bring it at least this much closer to the destination.
        public const float MinProgress = 1.5f;

        // detourShare: how much longer than the straight line (0.5 = 50%) the walk through the
        // bush may be. False when no bush qualifies: then it walks straight there.
        public static bool TryPick(Vector3 from, Vector3 goal, float detourShare, IReadOnlyList<Vector3> bushes, out Vector3 hop)
        {
            hop = default;
            float direct = Flat(goal - from);
            if (detourShare <= 0f || direct < MinRoute) return false;

            float budget = direct * (1f + detourShare);
            float best   = float.MaxValue;
            for (int i = 0; i < bushes.Count; i++)
            {
                Vector3 bush = bushes[i];
                float toBush = Flat(bush - from);
                if (toBush < MinHop || toBush >= best) continue;

                float onward = Flat(goal - bush);
                if (onward > direct - MinProgress || toBush + onward > budget) continue;

                best = toBush;
                hop  = bush;
            }
            return best < float.MaxValue;
        }

        // Bushes an enemy can hide in standing up (not crouch-only grass, not smoke).
        public static bool Hides(ConcealmentZone zone) =>
            zone != null && zone.Kind == ConcealmentKind.Bush && !zone.RequiresCrouch;

        // Where to pass through each of these bushes on the way from → goal (into `points`, cleared
        // first): out the far side of one the walk crosses, else the nearest part of it. Not the
        // middle — it goes in at the edge it meets and carries on across.
        public static void CollectPassPoints(Vector3 from, Vector3 goal, IReadOnlyList<BushFootprint> areas, List<Vector3> points)
        {
            points.Clear();
            for (int i = 0; i < areas.Count; i++)
                points.Add(BushApproach.PassPoint(from, goal, areas[i]));
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
