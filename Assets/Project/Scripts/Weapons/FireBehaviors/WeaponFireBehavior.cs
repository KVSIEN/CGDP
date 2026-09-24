using UnityEngine;
using CGD.Combat;
using CGD.Core;

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
            return new DamageInfo(ctx.Damage * damageScale, d.ArmorPenetration, d.DamageType,
                                  d.HeadshotMultiplier, ctx.Source, d.OnHitEffects);
        }

        // A weapon's range stats as a curve, so a shot resolved instantly and one resolved
        // mid-flight by a projectile scale damage by distance the same way.
        protected static DamageFalloff FalloffOf(WeaponData data) =>
            new(data.RangeOptimal, data.RangeFalloffEnd, data.DamageFalloffMin);

        protected static void FireHitscanRay(in FireContext ctx, Vector3 direction)
        {
            float range  = ctx.Data.EffectiveMaxRange;
            bool  didHit = Physics.Raycast(ctx.CameraPosition, direction, out RaycastHit hit,
                range, ctx.Data.HitMask, QueryTriggerInteraction.Ignore);

            if (ctx.DebugDraw)
            {
                Vector3 origin = ctx.Muzzle != null ? ctx.Muzzle.position : ctx.CameraPosition;
                Vector3 end    = didHit ? hit.point : origin + direction * range;
                Debug.DrawLine(origin, end, didHit ? ctx.DebugHitColor : ctx.DebugMissColor, ctx.DebugLineDuration);
            }

            if (didHit) ApplyHitDamage(hit, ctx);
        }

        protected static void ApplyHitDamage(RaycastHit hit, in FireContext ctx)
        {
            float falloff = FalloffOf(ctx.Data).Evaluate(hit.distance);
            Hitbox.ApplyHit(hit.collider, BuildDamageInfo(ctx, falloff), hit.point);
        }

        // Resolves one round in two stages. The stretch it would cross within a frame or two
        // resolves as a plain raycast, so point-blank fire registers on the frame the trigger
        // breaks; past that the shot becomes a real projectile that sweeps its own path each
        // frame. Both stages run along the same ray and share one falloff curve, so where a
        // shot happens to change hands is invisible to the player.
        protected static void FireProjectileShot(in FireContext ctx, Vector3 direction, in ProjectileShot shot)
        {
            if (!shot.IsValid) return;

            float maxRange     = ctx.Data.EffectiveMaxRange;
            float instantRange = Mathf.Min(shot.Speed * shot.InstantHitTime, maxRange);

            if (instantRange > 0f &&
                Physics.Raycast(ctx.CameraPosition, direction, out RaycastHit hit,
                                instantRange, ctx.Data.HitMask, QueryTriggerInteraction.Ignore))
            {
                ApplyHitDamage(hit, ctx);
                return;
            }

            // Nothing that close, so hand the rest of the flight to a projectile starting where
            // the raycast stopped — the two stages neither overlap nor leave a gap.
            Vector3    origin = ctx.CameraPosition + direction * instantRange;
            GameObject go     = PrefabPool.Spawn(shot.Prefab.gameObject, origin, Quaternion.LookRotation(direction));

            go.GetComponent<Projectile>().Launch(new ProjectileLaunch
            {
                Damage            = BuildDamageInfo(ctx),
                Velocity          = direction * shot.Speed,
                Gravity           = shot.Gravity,
                Lifetime          = shot.Lifetime,
                MaxDistance       = maxRange,
                HitMask           = ctx.Data.HitMask,
                Falloff           = FalloffOf(ctx.Data),
                DistanceTravelled = instantRange,
            });
        }
    }
}
