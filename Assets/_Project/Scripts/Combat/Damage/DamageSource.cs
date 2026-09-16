using UnityEngine;

namespace CGD.Combat
{
    // Who dealt a hit. The default value (no owner, Team.None) is environmental or
    // status-effect damage, which hits everyone.
    public readonly struct DamageSource
    {
        public readonly GameObject Owner;
        public readonly Team Team;

        public DamageSource(GameObject owner, Team team)
        {
            Owner = owner;
            Team  = team;
        }

        // Takes the team from the owner's HealthManager; owners without one have no team.
        public static DamageSource Of(GameObject owner)
        {
            var health = owner.GetComponentInParent<HealthManager>();
            return new DamageSource(owner, health != null ? health.Team : Team.None);
        }
    }
}
