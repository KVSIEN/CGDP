using UnityEngine;

namespace CGD.Combat
{
    // Pure debuff: slow + armor reduction per stack, stun at max stacks (Cryo, chills, snares).
    // Stateless — Tick() recomputes from stack count, safe on shared assets.
    // The target has one armor reduction, so only assets that reduce armor touch it: a
    // slow-only chill landing alongside a Cryo would otherwise wipe the Cryo's reduction.
    [CreateAssetMenu(fileName = "IceEffect", menuName = "CGD/Combat/Status Effects/Ice")]
    public class IceEffect : StatusEffect
    {
        [Header("Ice")]
        [Tooltip("Movement slow added per stack (0.05 = 5%)")]
        public float SlowPerStack = 0.05f;
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
            Stunnable stunnable = target.Stunnable;

            if (clampedStacks >= MaxStacks && stunnable != null && !stunnable.IsStunned)
                stunnable.ApplyStun(StunDuration);

            bool stunned = stunnable != null && stunnable.IsStunned;

            if (stunnable != null)
                stunnable.SetSlow(this, stunned ? 0f : 1f - clampedStacks * SlowPerStack);

            if (!ReducesArmor) return;

            float armorReduction = clampedStacks * ArmorReductionPerStack;
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
