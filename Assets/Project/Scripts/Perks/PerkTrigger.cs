namespace CGD.Perks
{
    // What sets a triggered perk off. A weapon's perks only answer while it is in hand, and the
    // hit triggers (Hit, CriticalHit, Kill) only when that weapon dealt the hit. Worn armor's
    // perks answer to every one of them, whatever dealt it. Values are explicit: assets store them.
    public enum PerkTrigger
    {
        Kill         = 0,
        Parry        = 1,
        Dodge        = 2,
        AbilityCast  = 3,
        DamageTaken  = 4,
        Healed       = 5,
        AimStart     = 6,
        ReloadStart  = 7,
        Reloaded     = 8,  // the rounds went in (a reload's commit point)
        Hit          = 9,
        CriticalHit  = 10,
        ItemUsed     = 11, // a consumable finished
        WeaponSwap   = 12, // the weapon was swapped to (armor: any swap)
    }
}
