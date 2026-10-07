using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // A bow's shot: PelletCount arrows loosed together, fanned out in a line along the
    // shot's VolleyAxis (sideways for a horizontal draw, up and down for a vertical one),
    // each then scattered by the normal spread cone. A single-arrow bow fires one arrow
    // straight, like a Projectile behavior. Extra arrows split the hit: each deals
    // 1/√n of the damage, so a volley rewards landing several arrows without letting a
    // point-blank triple shot triple the damage.
    [CreateAssetMenu(fileName = "VolleyBehavior", menuName = "CGD/Weapons/Fire Behaviors/Volley")]
    public class VolleyFireBehavior : WeaponFireBehavior
    {
        [Tooltip("Prefab for each arrow in flight")]
        [SerializeField] private Projectile _prefab;

        public override void Execute(FireContext ctx)
        {
            WeaponData d = ctx.Data;
            int arrows = Mathf.Max(1, d.PelletCount);
            var shot = new ProjectileShot(_prefab,
                                          ProjectileSpeedOf(ctx),
                                          d.GetProjectileGravity(ctx.Charge),
                                          d.ProjectileLifetime,
                                          d.ProjectileInstantHitTime);
            if (!shot.IsValid) return;

            if (arrows == 1)
            {
                FireProjectileShot(ctx, ctx.Direction, shot);
                return;
            }

            FireContext arrowCtx = ctx;
            arrowCtx.Damage = ctx.Damage / Mathf.Sqrt(arrows);
            float spacing = Mathf.Tan(d.VolleySpacingDeg * Mathf.Deg2Rad);

            for (int i = 0; i < arrows; i++)
            {
                float   offset = (i - (arrows - 1) * 0.5f) * spacing;
                Vector3 lane   = (ctx.CameraForward + ctx.VolleyAxis * offset).normalized;
                FireProjectileShot(arrowCtx, ComputeSpreadDirection(lane, ctx.SpreadDeg), shot);
            }
        }
    }
}
