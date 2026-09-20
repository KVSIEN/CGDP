namespace CGD.Items
{
    // Worn equipment slots from the GDD. Backslot is the versatile one (capes,
    // backpacks, quivers, power packs). Values are explicit so new slots can be
    // appended without renumbering what serialized assets already reference.
    public enum EquipmentSlot
    {
        Helm     = 0,
        Torso    = 1,
        Gloves   = 2,
        Legs     = 3,
        Boots    = 4,
        Backslot = 5,
    }
}
