using UnityEngine;
using CGD.Combat;
using CGD.Targeting;

namespace CGD.Abilities
{
    // Damages and/or heals whatever its TargetSelector picks: a cone blast on enemies, a
    // heal on allies around the player, a strike on the enemy under the crosshair. Not
    // used when it wouldn't affect anyone (no enemies in reach, allies all at full health),
    // so the charge and cost are kept.
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

        // Assets are shared, but abilities execute one at a time on the main thread.
        private readonly TargetSet _targets = new();

        public override bool CanExecute(AbilityContext ctx) => Select(ctx) > 0;

        public override void Execute(AbilityContext ctx)
        {
            if (Select(ctx) == 0) return;

            var hit = new DamageInfo(Damage, ArmorPenetration, DamageType, source: ctx.Source, onHitEffects: OnHitEffects);

            foreach (HealthManager target in _targets.Targets)
            {
                if (Damage > 0f) target.TakeDamage(hit);
                if (Heal   > 0f && !target.IsDead) target.Heal(Heal);
            }
        }

        // Team filtering in HealthManager would already make damage to allies a no-op; this
        // just keeps them out of the count that decides whether the ability is usable.
        private bool WouldAffect(HealthManager target, AbilityContext ctx) =>
            (Damage > 0f && target.CanBeDamagedBy(ctx.Source)) ||
            (Heal   > 0f && target.Health < target.MaxHealth);

        private int Select(AbilityContext ctx)
        {
            if (Targeting == null) return 0;

            Transform aim = ctx.CameraTransform != null ? ctx.CameraTransform : ctx.PlayerTransform;
            Targeting.Select(new TargetingRequest(aim.position, aim.forward, ctx.Health), _targets);

            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                if (!WouldAffect(_targets.Targets[i], ctx)) _targets.Targets.RemoveAt(i);
            }
            return _targets.Count;
        }
    }
}
