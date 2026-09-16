using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    public abstract class WeaponFireBehavior : ScriptableObject
    {
        public abstract void Execute(FireContext ctx);

        public static Vector3 ComputeSpreadDirection(Vector3 forward, float spreadDeg)
        {
            if (spreadDeg <= 0f) return forward;
            float radius = Mathf.Tan(spreadDeg * Mathf.Deg2Rad);
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            Quaternion rot = Quaternion.LookRotation(forward, up);
            return (rot * new Vector3(offset.x, offset.y, 1f)).normalized;
        }

        protected static void ApplyHitDamage(RaycastHit hit, WeaponData data)
        {
            float t       = Mathf.InverseLerp(data.RangeOptimal, data.RangeFalloffEnd, hit.distance);
            float falloff = Mathf.Lerp(1f, data.DamageFalloffMin, t);
            var   info    = new DamageInfo(data.Damage * falloff, criticalMultiplier: data.HeadshotMultiplier);
            Hitbox.ApplyHit(hit.collider, info, hit.point);
        }
    }
}
