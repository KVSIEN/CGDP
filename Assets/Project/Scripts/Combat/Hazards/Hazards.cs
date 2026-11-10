using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // Every damaging zone open right now, so characters can tell they are standing in one and
    // find the way out. Effects add their zone when it opens and remove it when it closes.
    public static class Hazards
    {
        private const int   ExitDirections = 12;
        private const float ExitMargin     = 1f;   // metres past the edge, so it is clearly out

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
