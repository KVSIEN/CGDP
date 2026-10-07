using UnityEngine;
using CGD.Combat;
using CGD.Items;

namespace CGD.Player
{
    // The player's health, armor and shield values, plus lifesteal.
    public class PlayerHealth : HealthManager
    {
        [Header("Health")]
        [SerializeField] private float _maxHealth = 100f;
        [Tooltip("Health on spawn and respawn")]
        [SerializeField] private float _health = 100f;

        [Header("Armor")]
        [SerializeField] private float _armor = 0f;

        [Header("Shield")]
        [SerializeField] private float _maxShield = 0f;
        [Tooltip("Seconds without taking damage before shield starts regenerating")]
        [SerializeField] private float _shieldRegenDelay = 5f;
        [Tooltip("Shield points restored per second once regen starts")]
        [SerializeField] private float _shieldRegenRate = 10f;

        public override float MaxHealth => WithModifiers(ItemStat.Health, _maxHealth);
        public override float Armor     => WithModifiers(ItemStat.Armor, _armor);
        public override float MaxShield => WithModifiers(ItemStat.Shield, _maxShield);
        protected override float ShieldRegenDelay => _shieldRegenDelay;
        protected override float ShieldRegenRate  => _shieldRegenRate;
        protected override float StartingHealth   => _health;

        public override Team Team => Team.Player;

        // Lifesteal: a share of the damage the player deals comes back as health — from gear
        // and buffs on the player, plus the Lifesteal of the weapon that dealt the hit.
        private void OnEnable()  => CombatEvents.DamageDealt += OnDamageDealt;
        private void OnDisable() => CombatEvents.DamageDealt -= OnDamageDealt;

        private void OnDamageDealt(DamageReport report)
        {
            if (report.Source.Owner != gameObject || report.Amount <= 0f || report.Target == this || IsDead) return;

            float weapon = report.Source.Weapon is ItemInstance item ? item.Modify(ItemStat.Lifesteal, 0f) : 0f;
            float share  = WithModifiers(ItemStat.Lifesteal, weapon);
            if (share > 0f) Heal(report.Amount * share);
        }
    }
}
