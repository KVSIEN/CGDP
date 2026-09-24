using UnityEngine;

namespace CGD.Combat
{
    // Health for breakable props — crates, barrels, generators. No team, so anyone's
    // attacks break it. Pair with DespawnOnDeath to remove the wreck and LootDropper to
    // spill its contents.
    public class Destructible : HealthManager
    {
        [SerializeField, Min(1f)] private float _maxHealth = 50f;
        [SerializeField, Min(0f)] private float _armor;

        public override Team  Team      => Team.None;
        public override float MaxHealth => _maxHealth;
        public override float Armor     => _armor;
        public override float MaxShield => 0f;
        protected override float ShieldRegenDelay => 0f;
        protected override float ShieldRegenRate  => 0f;
    }
}
