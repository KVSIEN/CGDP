namespace CGD.Loot
{
    public enum LootEntryKind
    {
        Item,   // an ItemDefinition: a counted stack, or rolled gear pieces
        Table,  // another LootTable, rolled in place (shared sub-tables, "one of these")
        Prefab, // any world object spawned as-is (health orbs, a mimic, an explosion)
    }
}
