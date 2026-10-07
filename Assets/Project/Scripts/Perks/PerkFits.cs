using System;

namespace CGD.Perks
{
    // Which gear a perk can roll on.
    [Flags]
    public enum PerkFits
    {
        Firearm = 1,
        Melee   = 2,
        Armor   = 4,
    }
}
