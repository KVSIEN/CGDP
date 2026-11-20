using UnityEngine;

namespace CGD.Combat
{
    // Pure debuff: slow + armor reduction per stack (both × Potency), stun at max stacks (Ice,
    // Cryo, the plain Slow).
    // Stateless — Tick() recomputes from stack count, safe on shared assets.
    // The target has one armor reduction, so only assets that reduce armor touch it: a
    // slow-only chill landing alongside a Cryo would otherwise wipe the Cryo's reduction.
    [CreateAssetMenu(fileName = "IceEffect", menuName = "CGD/Combat/Status Effects/Ice")]
    public class IceEffect : StatusEffect
    {
        [Header("Ice")]
        [Tooltip("Movement slow added per stack (0.05 = 5%)")]
        public float SlowPerStack = 0.05f;
        [Tooltip("The most it can slow, whatever the stacks and potency (1 = it can stop the target). Sources of different potency share the stacks, so cap it")]
        [Range(0f, 1f)] public float MaxSlow = 1f;
        [Tooltip("Armor reduction added per stack (0.02 = 2%)")]
        public float ArmorReductionPerStack = 0.02f;
        [Tooltip("Seconds the target is stunned for once it reaches MaxStacks")]
        public float StunDuration = 3f;
        [Tooltip("Extra armor reduction applied while stunned, on top of the per-stack total")]
        public float StunArmorReductionBonus = 0.10f;

        private bool ReducesArmor => ArmorReductionPerStack > 0f || StunArmorReductionBonus > 0f;

        public override void Tick(StatusEffectController target, int stacks, float magnitude)
        {
            int clampedStacks = Mathf.Min(stacks, MaxStacks);
            float potency = target.PotencyOf(this);
            Stunnable stunnable = target.Stunnable;

            if (clampedStacks >= MaxStacks && stunnable != null && !stunnable.IsStunned)
                stunnable.ApplyStun(StunDuration);

            bool stunned = stunnable != null && stunnable.IsStunned;

            if (stunnable != null)
                stunnable.SetSlow(this, stunned ? 0f : 1f - Mathf.Min(clampedStacks * SlowPerStack * potency, MaxSlow));

            if (!ReducesArmor) return;

            float armorReduction = clampedStacks * ArmorReductionPerStack * potency;
            if (stunned) armorReduction += StunArmorReductionBonus;
            target.Damageable.ArmorReductionPercent = armorReduction;
        }

        public override void OnRemoved(StatusEffectController target)
        {
            if (ReducesArmor) target.Damageable.ArmorReductionPercent = 0f;

            if (target.Stunnable != null)
                target.Stunnable.ClearSlow(this);
        }
    }
}
