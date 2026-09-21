using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Weapons
{
    // Two-stage shot. The stretch a round would cross within a frame or two resolves as a
    // plain raycast, so point-blank fire registers on the frame the trigger breaks; past
    // that the shot becomes a real projectile that sweeps its own path each frame. Both
    // stages run along the camera ray and share one falloff curve, so where a shot happens
    // to change hands is invisible to the player.
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
            if (_prefab == null) return;

            float charge  = Mathf.Clamp01(ctx.Charge);
            float speed   = _speed   * Mathf.Lerp(_lowChargeSpeedMultiplier,   1f, charge);
            float gravity = _gravity * Mathf.Lerp(_lowChargeGravityMultiplier, 1f, charge);

            float maxRange     = ctx.Data.EffectiveMaxRange;
            float instantRange = Mathf.Min(speed * _instantHitTime, maxRange);

            if (instantRange > 0f &&
                Physics.Raycast(ctx.CameraPosition, ctx.Direction, out RaycastHit hit,
                                instantRange, ctx.Data.HitMask, QueryTriggerInteraction.Ignore))
            {
                ApplyHitDamage(hit, ctx);
                return;
            }

            // Nothing that close, so hand the rest of the flight to a projectile starting where
            // the raycast stopped — the two stages neither overlap nor leave a gap.
            Vector3    origin = ctx.CameraPosition + ctx.Direction * instantRange;
            GameObject go     = PrefabPool.Spawn(_prefab.gameObject, origin, Quaternion.LookRotation(ctx.Direction));

            go.GetComponent<Projectile>().Launch(new ProjectileLaunch
            {
                Damage            = BuildDamageInfo(ctx),
                Velocity          = ctx.Direction * speed,
                Gravity           = gravity,
                Lifetime          = _lifetime,
                MaxDistance       = maxRange,
                HitMask           = ctx.Data.HitMask,
                Falloff           = FalloffOf(ctx.Data),
                DistanceTravelled = instantRange,
            });
        }
    }
}
