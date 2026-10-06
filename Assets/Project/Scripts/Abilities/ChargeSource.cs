namespace CGD.Abilities
{
    // What refills an ability's charges.
    public enum ChargeSource
    {
        Cooldown, // one charge per Cooldown, as usual
        Surge,    // one charge each time the ability's Surge gauge fills (it then empties)
        Both,     // either, whichever comes first
    }
}
