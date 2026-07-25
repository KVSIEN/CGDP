using UnityEngine;

// Bleed Damage (per tick) = (Raw Damage × RawDamageFraction) + (Target Max Health × MaxHealthFraction).
// Ignores armor entirely (DamageType.True). Non-stacking — MaxStacks stays at the
// StatusEffect default of 1, so reapplying just refreshes duration and magnitude.
[CreateAssetMenu(fileName = "BleedEffect", menuName = "CGD/Status Effects/Bleed")]
public class BleedEffect : StatusEffect
{
    [Header("Bleed")]
    [Tooltip("Fraction of the originating hit's raw damage dealt per tick")]
    public float RawDamageFraction = 0.5f;
    [Tooltip("Fraction of the target's max health dealt per tick")]
    public float MaxHealthFraction = 0.02f;

    public override void Tick(IDamageable target, int stacks, float magnitude)
    {
        float damage = magnitude * RawDamageFraction + target.MaxHealth * MaxHealthFraction;
        target.TakeDamage(new DamageInfo(damage, type: DamageType.True));
    }
}
