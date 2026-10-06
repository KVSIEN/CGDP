using System;

namespace CGD.Weapons
{
    // Which weapons a perk can roll on.
    [Flags]
    public enum PerkWeaponKinds
    {
        Firearm = 1,
        Melee   = 2,
    }
}
