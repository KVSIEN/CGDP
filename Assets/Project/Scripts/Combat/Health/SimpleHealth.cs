using UnityEngine;

namespace CGD.Combat
{
    // Health set in the Inspector and nothing more: no shield. For objects rather than
    // characters (props, deployables, objectives); each says only which side it is on.
    public abstract class SimpleHealth : HealthManager
    {
        [SerializeField, Min(1f)] private float _maxHealth;
        [SerializeField, Min(0f)] private float _armor;

        // The max health a newly added component starts with.
        protected SimpleHealth(float defaultMaxHealth) => _maxHealth = defaultMaxHealth;

        public override float MaxHealth => _maxHealth;
        public override float Armor     => _armor;
        public override float MaxShield => 0f;
        protected override float ShieldRegenDelay => 0f;
        protected override float ShieldRegenRate  => 0f;
    }
}
