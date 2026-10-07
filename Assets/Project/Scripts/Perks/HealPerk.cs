using UnityEngine;

namespace CGD.Perks
{
    // Restores health, as a flat amount and/or a share of maximum health.
    [CreateAssetMenu(fileName = "HealPerk", menuName = "CGD/Perks/Heal")]
    public class HealPerk : TriggeredPerk
    {
        [SerializeField, Min(0f)] private float _amount;
        [Tooltip("Share of maximum health (0.1 = 10%)")]
        [SerializeField, Range(0f, 1f)] private float _maxHealthFraction = 0.1f;

        public override bool Apply(PerkContext context)
        {
            var health = context.Health;
            if (health == null || health.IsDead || health.Health >= health.MaxHealth) return false;

            health.Heal(_amount + health.MaxHealth * _maxHealthFraction);
            return true;
        }
    }
}
