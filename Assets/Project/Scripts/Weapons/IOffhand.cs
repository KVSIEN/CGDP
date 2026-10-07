using System.Collections.Generic;
using CGD.Artifacts;
using CGD.Items;

namespace CGD.Weapons
{
    // Something the offhand slot can hold: a one-handed melee weapon, a shield or an artifact.
    // Only used while the main hand is one-handed or empty.
    public interface IOffhand
    {
        // What the Melee key swings (a weapon's combo) or bashes with (a shield), and for a
        // shield also how it blocks. Null for an artifact: V then bashes with the main hand.
        MeleeWeaponData OffhandData { get; }
        bool IsShield { get; }
        // A tower shield: blocks hits from in front on its own, leaving the aim input free.
        bool BlocksPassively { get; }
        // It is used on the aim input (a raised shield, an active artifact): a gun can't aim while
        // it is held, and a melee weapon's own guard gives way to it.
        bool TakesAim { get; }
        // An artifact's active use (null for everything else).
        OffhandUse Use { get; }
        // What holding it costs the main-hand gun, e.g. more spread and recoil.
        IReadOnlyList<StatModifier> MainHandPenalty { get; }
    }
}
