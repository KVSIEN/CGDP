using CGD.Combat;

namespace CGD.Weapons
{
    // One round's flight parameters after a behavior has resolved its own tuning (charge
    // scaling included). Lets a single- and a multi-pellet behavior hand the same shot
    // description to the shared two-stage resolver instead of each repeating the flight math.
    public readonly struct ProjectileShot
    {
        public readonly Projectile Prefab;
        public readonly float      Speed;          // m/s at the muzzle
        public readonly float      Gravity;        // downward acceleration in m/s² (0 = straight flight)
        public readonly float      Lifetime;       // seconds of flight before the round gives up
        // Flight time resolved instantly as a raycast before a projectile spawns; multiplied by
        // Speed to get that range, so faster rounds stay instant further out.
        public readonly float      InstantHitTime;

        public ProjectileShot(Projectile prefab, float speed, float gravity, float lifetime, float instantHitTime)
        {
            Prefab         = prefab;
            Speed          = speed;
            Gravity        = gravity;
            Lifetime       = lifetime;
            InstantHitTime = instantHitTime;
        }

        public bool IsValid => Prefab != null;
    }
}
