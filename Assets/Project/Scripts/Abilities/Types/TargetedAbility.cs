using UnityEngine;
using CGD.Combat;
using CGD.Targeting;

namespace CGD.Abilities
{
    // Damages and/or helps whatever its TargetSelector picks: a cone blast on enemies, a
    // heal, a shield top-up, a plating repair or a cleanse on allies around the caster, a strike on the enemy under
    // the crosshair. Not used when it wouldn't affect anyone (no enemies in reach, allies all
    // at full health and shield, nothing to cleanse), so the charge and cost are kept.
    [CreateAssetMenu(fileName = "TargetedAbility", menuName = "CGD/Abilities/Targeted")]
    public class TargetedAbility : Ability
    {
        [Header("Targeting")]
        public TargetSelector Targeting;

        [Header("Damage")]
        [Min(0f)] public float Damage = 25f;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        public DamageType DamageType = DamageType.Physical;
        public StatusEffectApplication[] OnHitEffects;

        [Header("Healing")]
        [Min(0f)] public float Heal = 0f;
        [Tooltip("Share of the damage dealt that comes back to the caster as health (a siphon). 1 = all of it; above 1 heals more than it took. 0 = none")]
        [Min(0f)] public float SelfHealShare = 0f;
        [Tooltip("Shield given back to each target, up to its maximum (a shield projector). 0 = none")]
        [Min(0f)] public float Shield = 0f;
        [Tooltip("Plating repaired on each target, up to its maximum (a field repair, regrown bone). 0 = none")]
        [Min(0f)] public float Plating = 0f;
        [Tooltip("On: also clears every status effect on each target (burns, bleeds, poison, chills, slows)")]
        public bool Cleanse;

        // Assets are shared, but abilities execute one at a time on the main thread.
        private readonly TargetSet _targets = new();

        public override bool CanExecute(AbilityContext ctx) => Select(ctx) > 0;

        public override bool ChecksAim => Targeting != null;

        public override TargetArea Area => Targeting != null ? Targeting.Area : TargetArea.None;

        public override void Execute(AbilityContext ctx)
        {
            if (Select(ctx) == 0) return;

            var hit = new DamageInfo(Scaled(Damage, ctx), ArmorPenetration, DamageType, source: ctx.Source,
                                     onHitEffects: WithBonusEffects(OnHitEffects, ctx));

            float dealt = 0f;
            foreach (HealthManager target in _targets.Targets)
            {
                if (Damage > 0f) dealt += DamageTaken(target, hit);
                if (target.IsDead) continue;
                if (Heal   > 0f) target.Heal(Heal);
                if (Shield > 0f) target.RestoreShield(Shield);
                if (Plating > 0f) target.RestorePlating(Plating);
                if (Cleanse && target.TryGetComponent(out StatusEffectController status)) status.Clear();
            }

            if (SelfHealShare > 0f && dealt > 0f && ctx.Health != null && !ctx.Health.IsDead)
                ctx.Health.Heal(dealt * SelfHealShare);
        }

        // What the hit actually took off the target (after armor, and only what it had left).
        private static float DamageTaken(HealthManager target, in DamageInfo hit)
        {
            float before = target.Health + target.Shield + target.Plating;
            target.TakeDamage(hit);
            return Mathf.Max(0f, before - (target.Health + target.Shield + target.Plating));
        }

        // Team filtering in HealthManager would already make damage to allies a no-op; this
        // just keeps them out of the count that decides whether the ability is usable.
        private bool WouldAffect(HealthManager target, AbilityContext ctx) =>
            (Damage > 0f && target.CanBeDamagedBy(ctx.Source)) ||
            (Heal   > 0f && target.Health < target.MaxHealth) ||
            (Shield > 0f && target.Shield < target.MaxShield) ||
            (Plating > 0f && target.Plating < target.MaxPlating) ||
            (Cleanse && target.TryGetComponent(out StatusEffectController status) && status.HasAny);

        private int Select(AbilityContext ctx)
        {
            if (Targeting == null) return 0;

            Transform aim = ctx.AimTransform != null ? ctx.AimTransform : ctx.CasterTransform;
            Targeting.Select(new TargetingRequest(aim.position, aim.forward, ctx.Health), _targets);

            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                if (!WouldAffect(_targets.Targets[i], ctx)) _targets.Targets.RemoveAt(i);
            }
            return _targets.Count;
        }
    }
}
