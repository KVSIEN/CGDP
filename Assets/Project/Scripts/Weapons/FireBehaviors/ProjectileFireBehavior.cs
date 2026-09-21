using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // Single two-stage shot: instant at point-blank range, a travelling round past that.
    // See WeaponFireBehavior.FireProjectileShot for how the two stages join up.
    [CreateAssetMenu(fileName = "ProjectileBehavior", menuName = "CGD/Weapons/Fire Behaviors/Projectile")]
    public class ProjectileFireBehavior : WeaponFireBehavior
    {
        [SerializeField] private Projectile _prefab;
        [Tooltip("Muzzle velocity at full charge (m/s)")]
        [SerializeField] private float _speed    = 60f;
        [SerializeField] private float _lifetime = 5f;
        [Tooltip("Downward acceleration in m/s² applied to the projectile at full charge. 0 = perfectly straight flight (bullets, plasma). Higher = arrow-like arc.")]
        [SerializeField] private float _gravity  = 0f;

        [Header("Instant Hit Window")]
        [Tooltip("Flight time under which a shot resolves instantly as a raycast instead of spawning a projectile. Multiplied by the shot's speed to get the range, so faster rounds stay instant further out. 0 = always simulate a projectile.")]
        [Min(0f)]
        [SerializeField] private float _instantHitTime = 0.02f;

        [Header("Charge Scaling")]
        [Tooltip("Speed multiplier when the shot is released at minimum charge. Ignored by non-Charge fire modes.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _lowChargeSpeedMultiplier   = 0.4f;
        [Tooltip("Gravity multiplier when the shot is released at minimum charge. Bows want this well above 1 so weak shots plummet.")]
        [Min(1f)]
        [SerializeField] private float _lowChargeGravityMultiplier = 3f;

        public override void Execute(FireContext ctx)
        {
            float charge  = Mathf.Clamp01(ctx.Charge);
            float speed   = _speed   * Mathf.Lerp(_lowChargeSpeedMultiplier,   1f, charge);
            float gravity = _gravity * Mathf.Lerp(_lowChargeGravityMultiplier, 1f, charge);

            FireProjectileShot(ctx, ctx.Direction,
                new ProjectileShot(_prefab, speed, gravity, _lifetime, _instantHitTime));
        }
    }
}
