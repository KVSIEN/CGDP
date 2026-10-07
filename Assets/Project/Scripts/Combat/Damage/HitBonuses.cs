namespace CGD.Combat
{
    // Extras an attacker's gear adds to one hit, resolved by the target. Default = none.
    public readonly struct HitBonuses
    {
        // A rolled critical (crit chance): counts as a critical hit wherever it lands, taking
        // the hit's CriticalMultiplier once (a headshot doesn't take it twice).
        public readonly bool  ForceCritical;
        // Added to each on-hit effect's chance as a fraction of it (0.3 = 30% likelier).
        public readonly float StatusChance;
        // Added to the strength of the status effects it applies (0.4 = 40% stronger).
        public readonly float StatusDamage;

        public HitBonuses(bool forceCritical, float statusChance, float statusDamage)
        {
            ForceCritical = forceCritical;
            StatusChance  = statusChance;
            StatusDamage  = statusDamage;
        }
    }
}
