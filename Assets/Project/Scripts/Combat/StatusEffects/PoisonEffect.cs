using UnityEngine;

namespace CGD.Combat
{
    // Poison Damage (per tick) = Effective Damage × (StackMultiplier ^ (Stacks − 1)).
    // Effective Damage is the originating hit's raw damage resolved through the
    // target's armor once; the exponential stack scaling is applied on top and
    // delivered via DamageType.True so it isn't mitigated a second time.
    // Uses Stack stacking; set MaxStacks on the asset to cap it (e.g. 5).
    [CreateAssetMenu(fileName = "PoisonEffect", menuName = "CGD/Combat/Status Effects/Poison")]
    public class PoisonEffect : StatusEffect
    {
        [Header("Poison")]
        [Tooltip("Damage multiplier applied per stack beyond the first")]
        public float StackMultiplier = 1.5f;

        public override void Tick(StatusEffectController target, int stacks, float magnitude)
        {
            float effectiveDamage = new DamageInfo(magnitude).ResolveDamage(target.Damageable.Armor);
            float damage = effectiveDamage * Mathf.Pow(StackMultiplier, stacks - 1);
            target.Damageable.TakeDamage(new DamageInfo(damage, type: DamageType.True));
        }
    }
}
