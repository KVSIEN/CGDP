using UnityEngine;

// Poison Damage (per tick) = Effective Damage × (StackMultiplier ^ (Stacks − 1)).
// Effective Damage is the originating hit's raw damage resolved through the
// target's armor once; the exponential stack scaling is applied on top and
// delivered via DamageType.True so it isn't mitigated a second time.
// Set MaxStacks on the asset to cap stacking (e.g. 5) or leave high for uncapped.
[CreateAssetMenu(fileName = "PoisonEffect", menuName = "CGD/Status Effects/Poison")]
public class PoisonEffect : StatusEffect
{
    [Header("Poison")]
    [Tooltip("Damage multiplier applied per stack beyond the first")]
    public float StackMultiplier = 1.5f;

    public override void Tick(IDamageable target, int stacks, float magnitude)
    {
        float effectiveDamage = new DamageInfo(magnitude).ResolveDamage(target.Armor);
        float damage = effectiveDamage * Mathf.Pow(StackMultiplier, stacks - 1);
        target.TakeDamage(new DamageInfo(damage, type: DamageType.True));
    }
}
