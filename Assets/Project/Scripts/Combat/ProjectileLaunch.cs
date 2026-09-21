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
        public DamageFalloff Falloff;
        // Metres already resolved before the projectile spawned, so falloff and max range stay
        // continuous across a shot that began life as a hitscan.
        public float         DistanceTravelled;
    }
}
