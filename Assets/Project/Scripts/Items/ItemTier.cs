namespace CGD.Items
{
    // Quality bands from the GDD's item quality table. A tier fixes the quality
    // window an item rolls inside; the roll decides where within it the item lands.
    // Stackable items (resources, consumables, munitions) carry a tier directly and
    // never roll — a higher-tier variant is a separate definition, not a better roll.
    public enum ItemTier
    {
        Common    = 0,
        Uncommon  = 1,
        Rare      = 2,
        Epic      = 3,
        Legendary = 4,
    }
}
