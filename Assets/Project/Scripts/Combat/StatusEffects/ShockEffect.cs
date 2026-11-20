using UnityEngine;

namespace CGD.Combat
{
    // Shock Damage (per jolt) = Raw Damage × JoltDamageFraction × Stacks × Potency, dealt as Lightning (so
    // it bites harder into shields). Every jolt also stuns the target for a split second, cutting
    // off whatever it was doing — a swing, a cast, a sprint. Tick() is the jolt.
    [CreateAssetMenu(fileName = "ShockEffect", menuName = "CGD/Combat/Status Effects/Shock")]
    public class ShockEffect : StatusEffect
    {
        [Header("Shock")]
        [Tooltip("Fraction of the originating hit's raw damage each jolt deals, per stack")]
        public float JoltDamageFraction = 0.15f;
        [Tooltip("Seconds each jolt stuns the target for (0 = no stun)")]
        [Min(0f)] public float JoltStun = 0.25f;

        public override void Tick(StatusEffectController target, int stacks, float magnitude)
        {
            if (JoltStun > 0f && target.Stunnable != null) target.Stunnable.ApplyStun(JoltStun);

            float damage = magnitude * JoltDamageFraction * stacks * target.PotencyOf(this);
            if (damage > 0f)
                target.Damageable.TakeDamage(new DamageInfo(damage, type: DamageType.Lightning, instigator: target.InstigatorOf(this)));
        }
    }
}
