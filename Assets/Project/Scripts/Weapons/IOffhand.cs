using System.Collections.Generic;
using CGD.Items;

namespace CGD.Weapons
{
    // Something the offhand slot can hold: a one-handed melee weapon or a shield. Only used
    // while the main hand is one-handed or empty.
    public interface IOffhand
    {
        // What the Melee key swings (a weapon's combo) or bashes with (a shield), and for a
        // shield also how it blocks.
        MeleeWeaponData OffhandData { get; }
        bool IsShield { get; }
        // A tower shield: blocks hits from in front on its own, leaving the aim input free.
        bool BlocksPassively { get; }
        // What holding it costs the main-hand gun, e.g. more spread and recoil.
        IReadOnlyList<StatModifier> MainHandPenalty { get; }
    }
}
