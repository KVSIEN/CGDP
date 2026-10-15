namespace CGD.Director
{
    // A snapshot of what the player is running low on, for the SupplyPlanner.
    public readonly struct SupplyNeeds
    {
        public SupplyNeeds(float healthShare, int healingCarried, float reserveMagazines)
        {
            HealthShare      = healthShare;
            HealingCarried   = healingCarried;
            ReserveMagazines = reserveMagazines;
        }

        // 0..1 of max health.
        public float HealthShare      { get; }
        // Healing items in the pack.
        public int   HealingCarried   { get; }
        // Spare ammo for the weapon in hand, in magazines; +infinity when it needs none.
        public float ReserveMagazines { get; }
    }
}
