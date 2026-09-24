using UnityEngine;
using CGD.Combat;
using CGD.Items;

namespace CGD.Player
{
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
        public override float MaxShield => _maxShield;
        protected override float ShieldRegenDelay => _shieldRegenDelay;
        protected override float ShieldRegenRate  => _shieldRegenRate;
        protected override float StartingHealth   => _health;

        public override Team Team => Team.Player;
    }
}
