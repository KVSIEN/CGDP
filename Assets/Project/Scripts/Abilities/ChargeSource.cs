namespace CGD.Abilities
{
    // What refills an ability's charges.
    public enum ChargeSource
    {
        Cooldown, // one charge per Cooldown, as usual
        Earned,   // one charge per EarnedPerCharge of the charge meter gained in combat
        Both,     // either, whichever comes first
    }
}
