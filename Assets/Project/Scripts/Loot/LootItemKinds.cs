using System;

namespace CGD.Loot
{
    // What a LootFilter lets through, by what an item is.
    [Flags]
    public enum LootItemKinds
    {
        None       = 0,
        Firearm    = 1 << 0,
        Melee      = 1 << 1,
        Armor      = 1 << 2,
        Shield     = 1 << 3,
        Artifact   = 1 << 4,
        Attachment = 1 << 5,
        Healing    = 1 << 6,   // consumables that restore health
        Buff       = 1 << 7,   // consumables that don't (stims)
        Throwable  = 1 << 8,
        Munition   = 1 << 9,
        Resource   = 1 << 10,
    }
}
