using UnityEngine;

// Pure debuff — deals no damage. Each stack grants SlowPerStack movement slow and
// ArmorReductionPerStack armor reduction; at MaxStacks the target is stunned for
// StunDuration with an extra ArmorReductionBonus, reaching 100% slow / 20% armor
// reduction while stunned. Tick() recomputes totals from the current stack count
// each call, so decay (see below) self-corrects automatically without Ice needing
// to track any state of its own — required since this asset is shared across every
// target it's applied to.
// Set MaxStacks to 5 and DecayOneStackAtATime to true on the asset to match the
// spec (5% slow / 2% armor reduction per stack, one stack lost every Duration
// seconds without a new one).
[CreateAssetMenu(fileName = "IceEffect", menuName = "CGD/Status Effects/Ice")]
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

    public override void Tick(IDamageable target, int stacks, float magnitude)
    {
        int clampedStacks = Mathf.Min(stacks, MaxStacks);
        var component = target as Component;
        IStunnable stunnable = component != null ? component.GetComponent<IStunnable>() : null;

        if (clampedStacks >= MaxStacks && stunnable != null && !stunnable.IsStunned)
            stunnable.ApplyStun(StunDuration);

        bool stunned = stunnable != null && stunnable.IsStunned;

        if (stunnable != null)
            stunnable.SpeedMultiplier = stunned ? 0f : 1f - clampedStacks * SlowPerStack;

        float armorReduction = clampedStacks * ArmorReductionPerStack;
        if (stunned) armorReduction += StunArmorReductionBonus;
        target.ArmorReductionPercent = armorReduction;
    }

    public override void OnRemoved(IDamageable target)
    {
        target.ArmorReductionPercent = 0f;

        var component = target as Component;
        IStunnable stunnable = component != null ? component.GetComponent<IStunnable>() : null;
        if (stunnable != null)
            stunnable.SpeedMultiplier = 1f;
    }
}
