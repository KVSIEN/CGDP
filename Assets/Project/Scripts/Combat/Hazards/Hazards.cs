using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // Every damaging zone open right now, so characters can tell they are standing in one and
    // find the way out. Effects add their zone when it opens and remove it when it closes.
    public static class Hazards
    {
        private const int   ExitDirections  = 12;
        private const float ExitMargin      = 1f;   // metres past the edge, so it is clearly out
        private const int   RoundDirections = 16;

        private static readonly List<HazardZone> Open = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Open.Clear();

        public static void Add(HazardZone zone)
        {
            if (zone != null && !Open.Contains(zone)) Open.Add(zone);
        }

        public static void Remove(HazardZone zone) => Open.Remove(zone);

        // Damage a second the zones at `position` would deal `victim` (its own side's don't count).
        public static float DamagePerSecondAt(Vector3 position, HealthManager victim)
        {
            float total = 0f;
            foreach (HazardZone zone in Open)
                if (zone.Covers(position) && zone.Hurts(victim)) total += zone.DamagePerSecond;
            return total;
        }

        // Whether any open zone would hurt `victim`, so route checks can be skipped while none would.
        public static bool AnyHurting(HealthManager victim)
        {
            foreach (HazardZone zone in Open)
                if (zone.Hurts(victim)) return true;
            return false;
        }

        // A zone hurting `victim` that covers `position`, if any.
        public static bool TryFindAt(Vector3 position, HealthManager victim, out HazardZone found)
        {
            foreach (HazardZone zone in Open)
            {
                if (!zone.Covers(position) || !zone.Hurts(victim)) continue;
                found = zone;
                return true;
            }
            found = null;
            return false;
        }

        // The first zone hurting `victim` that the straight line from → to passes through (within
        // `margin` of its edge). Zones `from` already stands in don't count: leaving one isn't crossing it.
        public static bool TryFindCrossing(Vector3 from, Vector3 to, HealthManager victim, float margin, out HazardZone first)
        {
            first = null;
            float nearest = float.MaxValue;
            foreach (HazardZone zone in Open)
            {
                if (!zone.Hurts(victim) || zone.Covers(from)) continue;
                if (!zone.Crosses(from, to, margin, out float along) || along >= nearest) continue;

                nearest = along;
                first   = zone;
            }
            return first != null;
        }

        // A point `margin` clear of `zone`'s edge to go round it by on the way from → to: the one
        // giving the shortest two straight legs that cross no zone hurting `victim` and that
        // `usable` accepts (e.g. on the NavMesh). False if there is no way round.
        public static bool TryFindWayRound(HazardZone zone, Vector3 from, Vector3 to, HealthManager victim, float margin,
                                           Func<Vector3, bool> usable, out Vector3 waypoint)
        {
            waypoint = default;
            float best = float.MaxValue;
            // Legs are checked a little inside the waypoint ring, or every leg would graze it.
            float legMargin = margin * 0.5f;
            for (int i = 0; i < RoundDirections; i++)
            {
                float angle = i * Mathf.PI * 2f / RoundDirections;
                Vector3 candidate = zone.Center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (zone.Radius + margin);
                candidate.y = from.y;

                float length = Vector3.Distance(from, candidate) + Vector3.Distance(candidate, to);
                if (length >= best || DamagePerSecondAt(candidate, victim) > 0f) continue;
                if (TryFindCrossing(from, candidate, victim, legMargin, out _) ||
                    TryFindCrossing(candidate, to, victim, legMargin, out _) || !usable(candidate)) continue;

                best     = length;
                waypoint = candidate;
            }
            return best < float.MaxValue;
        }

        // The nearest spot just past the edge of the zones hurting `victim` at `position` that is
        // clear of all of them and that `usable` accepts (e.g. on the NavMesh). False if none.
        public static bool TryFindWayOut(Vector3 position, HealthManager victim, Func<Vector3, bool> usable, out Vector3 exit)
        {
            exit = default;
            float best = float.MaxValue;
            foreach (HazardZone zone in Open)
            {
                if (!zone.Covers(position) || !zone.Hurts(victim)) continue;

                for (int i = 0; i < ExitDirections; i++)
                {
                    float angle = i * Mathf.PI * 2f / ExitDirections;
                    Vector3 candidate = zone.Center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (zone.Radius + ExitMargin);
                    candidate.y = position.y;

                    float distance = (candidate - position).sqrMagnitude;
                    if (distance >= best || DamagePerSecondAt(candidate, victim) > 0f || !usable(candidate)) continue;
                    best = distance;
                    exit = candidate;
                }
            }
            return best < float.MaxValue;
        }
    }
}
