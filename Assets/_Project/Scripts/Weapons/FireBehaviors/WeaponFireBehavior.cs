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

        // The one place a fire behavior turns weapon stats into a DamageInfo. Falloff for
        // hitscan and pellets goes through the damageScale; projectiles pass 1f and carry
        // the info forward until impact.
        protected static DamageInfo BuildDamageInfo(in FireContext ctx, float damageScale = 1f)
        {
            WeaponData d = ctx.Data;
            return new DamageInfo(d.Damage * damageScale, d.ArmorPenetration, d.DamageType,
                                  d.HeadshotMultiplier, ctx.Source, d.OnHitEffects);
        }

        protected static void ApplyHitDamage(RaycastHit hit, in FireContext ctx)
        {
            WeaponData data = ctx.Data;
            float t       = Mathf.InverseLerp(data.RangeOptimal, data.RangeFalloffEnd, hit.distance);
            float falloff = Mathf.Lerp(1f, data.DamageFalloffMin, t);
            Hitbox.ApplyHit(hit.collider, BuildDamageInfo(ctx, falloff), hit.point);
        }
    }
}
