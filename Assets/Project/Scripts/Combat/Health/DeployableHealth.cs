using UnityEngine;

namespace CGD.Combat
{
    // Health for something a character puts down (a Tech survey beacon): it fights for its
    // deployer's side, so that side can't break it and everyone else can — and sees it as a
    // hostile worth shooting. The team is set each time it is deployed.
    public class DeployableHealth : HealthManager
    {
        [SerializeField, Min(1f)] private float _maxHealth = 60f;
        [SerializeField, Min(0f)] private float _armor;

        private Team _team = Team.None;

        public override Team  Team      => _team;
        public override float MaxHealth => _maxHealth;
        public override float Armor     => _armor;
        public override float MaxShield => 0f;
        protected override float ShieldRegenDelay => 0f;
        protected override float ShieldRegenRate  => 0f;

        // Put down fresh for `team`: full health again (it may be a pooled instance that was broken).
        public void Deploy(Team team)
        {
            _team = team;
            Revive();
        }
    }
}
