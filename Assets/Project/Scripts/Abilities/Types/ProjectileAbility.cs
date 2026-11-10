using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Abilities
{
    [CreateAssetMenu(fileName = "ProjectileAbility", menuName = "CGD/Abilities/Projectile")]
    public class ProjectileAbility : Ability
    {
        [Tooltip("Prefab with a Projectile component")]
        public GameObject ProjectilePrefab;

        [Header("Projectile")]
        public float Damage   = 25f;
        public float Speed    = 25f;
        public float Lifetime = 5f;
        [Tooltip("Downward acceleration in m/s² applied to the projectile in flight. 0 = perfectly straight.")]
        [Min(0f)]
        public float Gravity  = 0f;
        [Tooltip("Metres: thickness of the projectile, so near misses still hit. 0 = a thin ray")]
        [Min(0f)] public float HitRadius = 0f;
        [Tooltip("Metres: everyone this close to where it lands is hurt (and affected) too. 0 = none")]
        [Min(0f)] public float BlastRadius = 0f;
        public StatusEffectApplication[] OnHitEffects;

        // Distance in front of the camera to spawn — prevents clipping through geometry directly ahead
        public float SpawnOffset = 1.5f;

        public override bool CanExecute(AbilityContext ctx) =>
            ProjectilePrefab != null && ProjectilePrefab.TryGetComponent<Projectile>(out _);

        public override void Execute(AbilityContext ctx)
        {
            Vector3 spawnPos = ctx.AimTransform.position + ctx.AimTransform.forward * SpawnOffset;
            var go = PrefabPool.Spawn(ProjectilePrefab, spawnPos, ctx.AimTransform.rotation);

            // The projectile ignores its source's colliders, so it can't hit the caster.
            // Abilities carry no range stats, so the shot neither falls off nor expires by
            // distance — Lifetime alone bounds it.
            var hit = new DamageInfo(Scaled(Damage, ctx), source: ctx.Source, onHitEffects: WithBonusEffects(OnHitEffects, ctx));
            go.GetComponent<Projectile>().Launch(new ProjectileLaunch
            {
                Damage      = hit,
                Velocity    = ctx.AimTransform.forward * Speed,
                Gravity     = Gravity,
                Lifetime    = Lifetime,
                MaxDistance = Mathf.Infinity,
                HitMask     = ~0,
                Falloff     = DamageFalloff.None,
                HitRadius   = HitRadius,
                BlastRadius = BlastRadius,
            });
        }
    }
}
