using UnityEngine;

namespace CGD.Combat
{
    // Who dealt a hit. The default value (no owner, Team.None) is environmental or
    // status-effect damage, which hits everyone.
    public readonly struct DamageSource
    {
        public readonly GameObject Owner;
        public readonly Team Team;
        // What the owner dealt it with (the carried weapon item), so a kill can be credited to
        // that weapon's perks. Null for fists, abilities, grenades and anything untracked.
        public readonly object Weapon;

        public DamageSource(GameObject owner, Team team, object weapon = null)
        {
            Owner  = owner;
            Team   = team;
            Weapon = weapon;
        }

        // Takes the team from the owner's HealthManager; owners without one have no team.
        public static DamageSource Of(GameObject owner)
        {
            var health = owner.GetComponentInParent<HealthManager>();
            return new DamageSource(owner, health != null ? health.Team : Team.None);
        }

        public DamageSource WithWeapon(object weapon) => new(Owner, Team, weapon);
    }
}
