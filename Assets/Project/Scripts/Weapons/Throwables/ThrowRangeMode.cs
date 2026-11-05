namespace CGD.Weapons
{
    // How the throw button decides how far a grenade goes.
    public enum ThrowRangeMode
    {
        // Holding the button longer throws further, up to the grenade's charge time.
        Charged,
        // Every throw goes the same distance however long the button is held (Call of Duty style).
        Fixed
    }
}
