namespace CGD.Items
{
    // The GDD's ammo economies. None covers melee; Cooldown covers VOID relics,
    // which are paced by time rather than supply and so never draw from a pool.
    public enum AmmoType
    {
        None        = 0,
        Arrows      = 1,
        Bullets     = 2,
        EnergyCells = 3,
        Cooldown    = 4,
    }
}
