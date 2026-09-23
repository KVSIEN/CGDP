namespace CGD.Items
{
    // Every stat an item can carry, covering both the GDD's weapon and armor tables.
    // Shared entries (crit, status) are deliberately one value so a stat means the
    // same thing wherever it appears.
    //
    // Values are explicit and contiguous: explicit so reordering can't corrupt
    // serialized assets, contiguous so StatBlock can index by (int)stat.
    // Append new stats at the end and bump nothing else.
    public enum ItemStat
    {
        None = 0,

        Damage           = 1,
        FireRate         = 2,
        CritChance       = 3,
        CritDamage       = 4,
        StatusChance     = 5,
        StatusDamage     = 6,
        Lifesteal        = 7,
        ArmorPenetration = 8,
        Range            = 9,
        MagazineSize     = 10,
        AmmoReserve      = 11,
        ReloadTime       = 12,
        Recoil           = 13,
        Spread           = 14,
        DrawTime         = 15,
        Sway             = 16,

        Health            = 17,
        Armor             = 18,
        Shield            = 19,
        CooldownReduction = 20,
        StatusResistance  = 21,
        MovementSpeed     = 22,
        HealthRegen       = 23,
        ShieldRegenRate   = 24,
    }
}
