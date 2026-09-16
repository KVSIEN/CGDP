using UnityEngine;

namespace CGD.Combat
{
    // Fire Damage (per tick) = Effective Damage × FireDpsPercent. Effective Damage
    // is the originating hit's raw damage resolved through the target's armor, so
    // armor still reduces fire ticks. Uses Refresh stacking; reapplying just refreshes
    // duration and magnitude.
    // Different weapons/abilities express their own Fire DPS% by referencing
    // different FireEffect assets, the same way WeaponFireBehavior variants work.
    [CreateAssetMenu(fileName = "FireEffect", menuName = "CGD/Combat/Status Effects/Fire")]
    public class FireEffect : StatusEffect
    {
        [Header("Fire")]
        [Tooltip("Fraction of Effective Damage dealt per tick")]
        public float FireDpsPercent = 0.2f;

        public override void Tick(StatusEffectController target, int stacks, float magnitude)
        {
            float effectiveDamage = new DamageInfo(magnitude).ResolveDamage(target.Damageable.Armor);
            float damage = effectiveDamage * FireDpsPercent;
            target.Damageable.TakeDamage(new DamageInfo(damage, type: DamageType.True));
        }
    }
}
