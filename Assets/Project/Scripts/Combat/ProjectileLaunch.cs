using UnityEngine;

namespace CGD.Combat
{
    // Everything a projectile needs for its flight, grouped so the three floats can't be
    // transposed silently at the call site.
    public struct ProjectileLaunch
    {
        public DamageInfo    Damage;
        public Vector3       Velocity;    // metres per second: direction × speed at spawn
        public float         Gravity;     // downward acceleration in m/s² (0 = straight flight)
        public float         Lifetime;    // seconds of flight before the round gives up
        public float         MaxDistance; // metres of flight before the round gives up
        public LayerMask     HitMask;
        // Thickness of the round: 0 = a thin ray, otherwise it sweeps a sphere this wide, so near misses hit.
        public float         HitRadius;
        // Metres around the impact point where everyone else is hurt too, by the same damage (0 = none).
        public float         BlastRadius;
        // Targets it passes through, hurting each, before the next one stops it (0 = the first stops it). Scenery always does.
        public int           Pierce;
        public DamageFalloff Falloff;
        // Metres already resolved before the projectile spawned, so falloff and max range stay
        // continuous across a shot that began life as a hitscan.
        public float         DistanceTravelled;
        // A lingering patch (the shared DamageZone prefab) it leaves where it lands, hitting what the
        // round hits and applying the round's own status effects. Null prefab or zero radius = none.
        public GameObject    ZonePrefab;
        public ZoneSettings  Zone;
    }
}
