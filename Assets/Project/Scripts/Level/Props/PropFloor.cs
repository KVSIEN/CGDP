namespace CGD.Level
{
    // Which floor level a prop stands on, in a room with raised or sunken floor
    // (ElevationStructureRule, TerracesStructureRule): the sofas down in a lounge pit, the drum
    // kit up on the stage. Levels are counted from the doorways' floor.
    public enum PropFloor
    {
        Any,
        Ground,   // the doorways' level
        Raised,   // above it: a platform, a stage, a mezzanine, a mound, an upper terrace
        Sunken,   // below it: a pit, a lounge well, a crater, a lower terrace
    }
}
