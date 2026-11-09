using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Abilities
{
    // Spends the whole shield in a burst round the caster: everything hostile nearby takes
    // damage in proportion to the shield given up, and any status effect listed (a slow).
    // Can't be cast with less than MinShield left.
    [CreateAssetMenu(fileName = "ShieldDischargeAbility", menuName = "CGD/Abilities/Shield Discharge")]
    public class ShieldDischargeAbility : Ability
    {
        [Min(0.1f)] public float Radius = 6f;
        [Tooltip("The shield that must be up to cast")]
        [Min(0f)] public float MinShield = 10f;
        [Tooltip("Damage dealt per point of shield spent")]
        [Min(0f)] public float DamagePerShield = 1.25f;
        public DamageType DamageType = DamageType.Lightning;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        [Tooltip("Applied to everyone hit (a slow)")]
        public StatusEffectApplication[] OnHitEffects;
        public LayerMask HitMask = ~0;

        private static readonly Collider[] _hitBuffer = new Collider[32];
        private static readonly HashSet<IDamageable> _damaged = new();

        public override bool CanExecute(AbilityContext ctx) => ctx.Health != null && ctx.Health.Shield >= MinShield;

        public override void Execute(AbilityContext ctx)
        {
            float spent = ctx.Health.DrainShield(ctx.Health.Shield);
            if (spent <= 0f) return;

            Vector3 origin = ctx.CasterTransform.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(origin, Radius, _hitBuffer, HitMask, QueryTriggerInteraction.Ignore);

            var hit = new DamageInfo(Scaled(spent * DamagePerShield, ctx), ArmorPenetration, DamageType,
                                     source: ctx.Source, onHitEffects: WithBonusEffects(OnHitEffects, ctx));
            _damaged.Clear();
            for (int i = 0; i < count; i++)
            {
                // Team filtering in HealthManager keeps the caster and its allies unharmed.
                IDamageable target = Hitbox.FindDamageable(_hitBuffer[i]);
                if (target != null && _damaged.Add(target)) target.TakeDamage(hit);
            }
        }
    }
}
