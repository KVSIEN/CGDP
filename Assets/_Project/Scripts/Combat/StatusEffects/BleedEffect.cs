using UnityEngine;

namespace CGD.Combat
{
    // Bleed Damage (per tick) = (Raw Damage × RawDamageFraction) + (Target Max Health × MaxHealthFraction).
    // Ignores armor entirely (DamageType.True). Uses Refresh stacking, so reapplying just
    // refreshes duration and magnitude.
    [CreateAssetMenu(fileName = "BleedEffect", menuName = "CGD/Combat/Status Effects/Bleed")]
    public class BleedEffect : StatusEffect
    {
        [Header("Bleed")]
        [Tooltip("Fraction of the originating hit's raw damage dealt per tick")]
        public float RawDamageFraction = 0.5f;
        [Tooltip("Fraction of the target's max health dealt per tick")]
        public float MaxHealthFraction = 0.02f;

        public override void Tick(StatusEffectController target, int stacks, float magnitude)
        {
            float damage = magnitude * RawDamageFraction + target.Damageable.MaxHealth * MaxHealthFraction;
            target.Damageable.TakeDamage(new DamageInfo(damage, type: DamageType.True));
        }
    }
}
