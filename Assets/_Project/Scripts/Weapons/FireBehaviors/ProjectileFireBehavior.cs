using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Weapons
{
    [CreateAssetMenu(fileName = "ProjectileBehavior", menuName = "CGD/Weapons/Fire Behaviors/Projectile")]
    public class ProjectileFireBehavior : WeaponFireBehavior
    {
        [SerializeField] private Projectile _prefab;
        [Tooltip("Muzzle velocity at full charge (m/s)")]
        [SerializeField] private float _speed    = 60f;
        [SerializeField] private float _lifetime = 5f;
        [Tooltip("Downward acceleration in m/s² applied to the projectile at full charge. 0 = perfectly straight flight (bullets, plasma). Higher = arrow-like arc.")]
        [SerializeField] private float _gravity  = 0f;

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

            Vector3    origin = ctx.Muzzle != null ? ctx.Muzzle.position : ctx.CameraPosition;
            GameObject go     = PrefabPool.Spawn(_prefab.gameObject, origin, Quaternion.LookRotation(ctx.Direction));

            go.GetComponent<Projectile>().Launch(BuildDamageInfo(ctx), ctx.Direction * speed, gravity, _lifetime);
        }
    }
}
