using UnityEngine;
using CGD.Combat;
using CGD.Stats;

namespace CGD.Abilities
{
    // Spends the whole shield in a burst round the caster: everything hostile nearby takes
    // damage in proportion to the shield given up, any status effect listed (a slow, a shock),
    // and is thrown back out of reach, while the caster gets a burst of speed (SelfBuff) — a
    // disengage that buys room to fall back.
    // Walls stop it (CoverMask): what is wholly behind one is spared, what still shows round
    // its edge or over its top is hit. Can't be cast with less than MinShield left.
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
        [Tooltip("Applied to everyone hit (Tech's slow and shock)")]
        public StatusEffectApplication[] OnHitEffects;
        public LayerMask HitMask = ~0;
        [Tooltip("Scenery that blocks it: a target wholly behind a wall is spared, one partly round it is still hit. Nothing = it reaches through walls")]
        public LayerMask CoverMask = 1;

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

        private static readonly AreaHits _area = new(32);

        public override bool CanExecute(AbilityContext ctx) => ctx.Health != null && ctx.Health.Shield >= MinShield;

        public override float EffectDuration => SelfBuff != null ? SelfBuffDuration : 0f;

        public override void Execute(AbilityContext ctx)
        {
            float spent = ctx.Health.DrainShield(ctx.Health.Shield);
            if (spent <= 0f) return;

            if (SelfBuff != null && ctx.Stats != null) ctx.Stats.AddTimed(SelfBuff, SelfBuffDuration);

            Vector3 center = ctx.CasterTransform.position;
            Vector3 origin = center + Vector3.up;
            _area.Gather(origin, Radius, HitMask, CoverMask);

            var hit = new DamageInfo(Scaled(spent * DamagePerShield, ctx), ArmorPenetration, DamageType,
                                     source: ctx.Source, onHitEffects: WithBonusEffects(OnHitEffects, ctx));
            foreach (AreaHit area in _area.Hits)
            {
                // Team filtering in HealthManager keeps the caster and its allies unharmed.
                IDamageable target = area.Target;
                target.TakeDamage(hit);
                if (target is HealthManager health && health.transform != ctx.CasterTransform)
                    Stunnable.PushAway(health, center, ctx.Source, KnockbackSpeed, KnockbackDuration);
            }
        }
    }
}
