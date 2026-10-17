using UnityEngine;
using CGD.Combat;

namespace CGD.Level
{
    // What a Siege room asks the player to defend. On the player's team, so enemies see it as
    // a hostile target like any other and attack it, while the player's own shots pass it by.
    // Its collider must be on a layer enemies look for targets on (the player's).
    public class SiegeObjective : HealthManager
    {
        [SerializeField, Min(1f)] private float _maxHealth = 400f;
        [SerializeField, Min(0f)] private float _armor;

        public override Team  Team      => Team.Player;
        public override float MaxHealth => _maxHealth;
        public override float Armor     => _armor;
        public override float MaxShield => 0f;
        protected override float ShieldRegenDelay => 0f;
        protected override float ShieldRegenRate  => 0f;
    }
}
