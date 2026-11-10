using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Stats;

namespace CGD.Abilities
{
    // Spends the whole shield in a burst round the caster: everything hostile nearby takes
    // damage in proportion to the shield given up, any status effect listed (a slow), and is
    // thrown back out of reach, while the caster gets a burst of speed (SelfBuff) — a
    // disengage that buys room to fall back.
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

        [Header("Knockback")]
        [Tooltip("m/s: how hard everyone hit is thrown away from the caster (eases out over the push). 0 = no knockback")]
        [Min(0f)] public float KnockbackSpeed = 14f;
        [Tooltip("Seconds the push lasts; they can't walk or act until it ends. Distance ≈ speed × duration ÷ 2")]
        [Min(0f)] public float KnockbackDuration = 0.4f;

        [Header("Caster")]
        [Tooltip("Buffs the caster as the shield goes (a burst of speed to get clear while it recharges). Optional")]
        public StatModifierPreset SelfBuff;
        [Tooltip("Game seconds the self buff lasts")]
        [Min(0.1f)] public float SelfBuffDuration = 3f;

        private static readonly Collider[] _hitBuffer = new Collider[32];
        private static readonly HashSet<IDamageable> _damaged = new();

        public override bool CanExecute(AbilityContext ctx) => ctx.Health != null && ctx.Health.Shield >= MinShield;

        public override float EffectDuration => SelfBuff != null ? SelfBuffDuration : 0f;

        public override void Execute(AbilityContext ctx)
        {
            float spent = ctx.Health.DrainShield(ctx.Health.Shield);
            if (spent <= 0f) return;

            if (SelfBuff != null && ctx.Stats != null) ctx.Stats.AddTimed(SelfBuff, SelfBuffDuration);

            Vector3 center = ctx.CasterTransform.position;
            Vector3 origin = center + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(origin, Radius, _hitBuffer, HitMask, QueryTriggerInteraction.Ignore);

            var hit = new DamageInfo(Scaled(spent * DamagePerShield, ctx), ArmorPenetration, DamageType,
                                     source: ctx.Source, onHitEffects: WithBonusEffects(OnHitEffects, ctx));
            _damaged.Clear();
            for (int i = 0; i < count; i++)
            {
                // Team filtering in HealthManager keeps the caster and its allies unharmed.
                IDamageable target = Hitbox.FindDamageable(_hitBuffer[i]);
                if (target == null || !_damaged.Add(target)) continue;

                target.TakeDamage(hit);
                if (target is HealthManager health && health.transform != ctx.CasterTransform)
                    TryKnockBack(health, center, ctx.Source);
            }
        }

        private void TryKnockBack(HealthManager target, Vector3 center, DamageSource source)
        {
            if (KnockbackSpeed <= 0f || target.IsDead || !target.CanBeDamagedBy(source)) return;
            if (!target.TryGetComponent(out Stunnable stunnable)) return;

            Vector3 away = target.transform.position - center;
            away.y = 0f;
            // Standing right on the caster (likely facing it): straight backwards.
            if (away.sqrMagnitude < 0.0001f) away = -target.transform.forward;
            stunnable.Knockback(away.normalized * KnockbackSpeed, KnockbackDuration);
        }
    }
}
