using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // Fires N independent pellets per shot, each down its own direction inside the spread cone.
    // Pellets resolve either as plain raycasts or as two-stage projectile rounds — the same
    // instant-hit-window flight a Projectile behavior gives a single round, applied per pellet.
    //
    // When projectile pellets are enabled the physics (speed, gravity, lifetime, instant-hit
    // window) come from the equipped WeaponData, so every shotgun tunes its own pellet
    // ballistics without cloning this behavior asset per weapon.
    [CreateAssetMenu(fileName = "ShotgunBehavior", menuName = "CGD/Weapons/Fire Behaviors/Shotgun")]
    public class ShotgunFireBehavior : WeaponFireBehavior
    {
        [Header("Projectile Pellets")]
        [Tooltip("Off: every pellet is an instant raycast. On: each pellet flies as a real projectile past the instant hit window on the equipped WeaponData, so the pellet cloud has to be led at range. Falls back to raycast pellets while no prefab is assigned.")]
        [SerializeField] private bool       _projectilePellets;
        [SerializeField] private Projectile _prefab;

        public override void Execute(FireContext ctx)
        {
            int pellets = Mathf.Max(1, ctx.Data.PelletCount);

            // Every pellet of a shot flies identically, so the description is built once.
            ProjectileShot shot = _projectilePellets && _prefab != null
                ? new ProjectileShot(_prefab,
                                     ctx.Data.GetProjectileSpeed(ctx.Charge),
                                     ctx.Data.GetProjectileGravity(ctx.Charge),
                                     ctx.Data.ProjectileLifetime,
                                     ctx.Data.ProjectileInstantHitTime)
                : default;

            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = ComputeSpreadDirection(ctx.CameraForward, ctx.SpreadDeg);
                if (shot.IsValid) FireProjectileShot(ctx, dir, shot);
                else              FireHitscanPellet(ctx, dir);
            }
        }

        private static void FireHitscanPellet(in FireContext ctx, Vector3 dir)
        {
            float range  = ctx.Data.EffectiveMaxRange;
            bool  didHit = Physics.Raycast(ctx.CameraPosition, dir, out RaycastHit hit,
                range, ctx.Data.HitMask, QueryTriggerInteraction.Ignore);

            if (ctx.DebugDraw)
            {
                Vector3 origin = ctx.Muzzle != null ? ctx.Muzzle.position : ctx.CameraPosition;
                Vector3 end    = didHit ? hit.point : origin + dir * range;
                Debug.DrawLine(origin, end, didHit ? ctx.DebugHitColor : ctx.DebugMissColor, ctx.DebugLineDuration);
            }

            if (didHit) ApplyHitDamage(hit, ctx);
        }
    }
}
