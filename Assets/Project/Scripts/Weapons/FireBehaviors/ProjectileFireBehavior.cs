using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // Single two-stage shot: instant at point-blank range, a travelling round past that.
    // Physics (speed, lifetime, gravity, instant-hit window, charge multipliers) live
    // on the equipped WeaponData so each weapon carries its own bullet velocity and
    // one behavior asset can serve every projectile-firing weapon in the game.
    // See WeaponFireBehavior.FireProjectileShot for how the two stages join up.
    [CreateAssetMenu(fileName = "ProjectileBehavior", menuName = "CGD/Weapons/Fire Behaviors/Projectile")]
    public class ProjectileFireBehavior : WeaponFireBehavior
    {
        [Tooltip("Prefab spawned for the travelling stage of the shot. Shared by every weapon that fires this projectile visual.")]
        [SerializeField] private Projectile _prefab;

        public override void Execute(FireContext ctx)
        {
            WeaponData d = ctx.Data;
            FireProjectileShot(ctx, ctx.Direction,
                new ProjectileShot(_prefab,
                                   d.GetProjectileSpeed(ctx.Charge),
                                   d.GetProjectileGravity(ctx.Charge),
                                   d.ProjectileLifetime,
                                   d.ProjectileInstantHitTime));
        }
    }
}
