namespace CGD.Level
{
    // What a raised or sunken piece of floor is, which decides where it goes and how it is reached.
    // Each rises or sinks a level, or in tiers several, a level per tier (ElevationStructureRule ›
    // Tiers, TerracesStructureRule › Tiers).
    public enum ElevationKind
    {
        Platform,     // up, set against a wall: a stage, a mezzanine, a loading deck; in tiers, a stepped stage
        SunkenFloor,  // down, out in the room with floor all round it: a pit, a lounge well; in tiers, a stepped pit
        Terrace,      // a lane running wall to wall a level up or down from the next (TerracesStructureRule)
        Mound,        // a round rise of ground, sloping all round: a hillock, a heap of rubble; in tiers, a terraced hill
        Crater,       // a round hollow, sloping all round: a blast crater, a sinkhole; in tiers, a pit ringed by ledges
        Section,      // half or a corner of the room lifted with whatever stands on it (RaisedSectionStructureRule)
        Island,       // up, out in the room with floor all round it: a kitchen island, a raised bar, a dais in the round; in tiers, a stepped island
    }
}
