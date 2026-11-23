using UnityEngine;

namespace CGD.Combat
{
    // A latched bite feeding: each tick it takes health off the target and gives a share of what
    // it actually took back to whoever applied it (a lifesteal). Pair it with a Restrain on the same
    // hit: it stops feeding the moment a restrainable target (the player) breaks free. Uses Refresh
    // stacking, so a second bite restarts it rather than doubling it.
    [CreateAssetMenu(fileName = "DrainEffect", menuName = "CGD/Combat/Status Effects/Drain")]
    public class DrainEffect : StatusEffect
    {
        [Header("Drain")]
        [Tooltip("Damage taken per tick at Potency 1 (ordinary damage: armour, plating and shield soak it first)")]
        [Min(0f)] public float DamagePerTick = 6f;
        [Tooltip("Share of the damage actually taken that heals whoever applied it")]
        [Range(0f, 2f)] public float LifestealShare = 0.6f;

        public override void Tick(StatusEffectController target, int stacks, float magnitude)
        {
            if (target.TryGetComponent(out IRestrainable restrainable) && !restrainable.IsRestrained) return;

            DamageSource source = target.InstigatorOf(this);
            float before = target.Damageable is HealthManager health ? health.Health + health.Shield + health.Plating : 0f;
            target.Damageable.TakeDamage(new DamageInfo(DamagePerTick * target.PotencyOf(this), instigator: source));
            if (target.Damageable is not HealthManager after) return;

            float taken = Mathf.Max(0f, before - (after.Health + after.Shield + after.Plating));
            if (taken <= 0f || source.Owner == null) return;

            var feeder = source.Owner.GetComponentInParent<HealthManager>();
            if (feeder != null && !feeder.IsDead) feeder.Heal(taken * LifestealShare);
        }
    }
}
