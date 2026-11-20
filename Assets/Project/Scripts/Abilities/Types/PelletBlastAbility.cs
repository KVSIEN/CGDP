using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Abilities
{
    // A shotgun blast: a spray of pellets fired at once, each down its own direction inside a
    // cone around the aim, flying as real (dodgeable) projectiles for a short range. Each pellet
    // that lands is its own hit, so its on-hit effects roll per pellet: the more of the spray
    // lands, the likelier a proc.
    [CreateAssetMenu(fileName = "PelletBlastAbility", menuName = "CGD/Abilities/Pellet Blast")]
    public class PelletBlastAbility : Ability
    {
        [Tooltip("Prefab with a Projectile component, fired once per pellet")]
        public GameObject PelletPrefab;

        [Header("Pellets")]
        [Min(1)] public int PelletCount = 8;
        [Tooltip("Degrees a pellet may stray from the aim (half the cone's width)")]
        [Range(0f, 45f)] public float SpreadAngle = 8f;
        [Min(0f)] public float DamagePerPellet = 5f;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        public DamageType DamageType = DamageType.Physical;
        [Min(1f)] public float Speed = 45f;
        [Tooltip("Metres a pellet flies before it gives up")]
        [Min(0.5f)] public float Range = 14f;
        [Tooltip("Metres: thickness of a pellet, so near misses still hit. 0 = a thin ray")]
        [Min(0f)] public float HitRadius = 0.1f;
        [Tooltip("Rolled for every pellet that hits")]
        public StatusEffectApplication[] OnHitEffects;

        [Tooltip("Distance in front of the aim the pellets spawn, so they clear the caster's own body")]
        [Min(0f)] public float SpawnOffset = 0.8f;

        public override bool CanExecute(AbilityContext ctx) =>
            PelletPrefab != null && PelletPrefab.TryGetComponent<Projectile>(out _);

        public override void Execute(AbilityContext ctx)
        {
            Transform aim = ctx.AimTransform;
            Vector3 spawnPos = aim.position + aim.forward * SpawnOffset;

            // Every pellet carries the same hit; the projectile ignores its source's colliders.
            var hit = new DamageInfo(Scaled(DamagePerPellet, ctx), ArmorPenetration, DamageType,
                                     source: ctx.Source, onHitEffects: WithBonusEffects(OnHitEffects, ctx));

            for (int i = 0; i < PelletCount; i++)
            {
                Vector3 direction = WeaponFireBehavior.ComputeSpreadDirection(aim.forward, SpreadAngle);
                var pellet = PrefabPool.Spawn(PelletPrefab, spawnPos, Quaternion.LookRotation(direction));
                pellet.GetComponent<Projectile>().Launch(new ProjectileLaunch
                {
                    Damage      = hit,
                    Velocity    = direction * Speed,
                    Lifetime    = Range / Speed,
                    MaxDistance = Range,
                    HitMask     = ~0,
                    Falloff     = DamageFalloff.None,
                    HitRadius   = HitRadius,
                });
            }
        }
    }
}
