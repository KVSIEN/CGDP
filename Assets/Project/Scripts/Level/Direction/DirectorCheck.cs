namespace CGD.Level
{
    // What the map director checks a map for (DirectorFinding). The pacing checks read the map
    // graph's main path before anything is built (MapDirector); the beat checks read the rooms
    // as they were built (BeatReview).
    public enum DirectorCheck
    {
        FlatStretch,     // too many ordinary fights in a row on the main path, no peak among them
        NoPeak,          // a main path with fights but no peak before the boss
        CalmStretch,     // too many calm rooms in a row on the main path
        EarlyPeak,       // the main path's strongest peak comes in its first third, and the rest only fall away
        PeakCover,       // a peak (or the boss room) holds no more cover for its size than a fight next to it
        PeakSize,        // a peak (or the boss room) is smaller than a fight next to it
        ReliefSize,      // a relief is no smaller than the peak it follows
        SuppliesMissing, // a beat's supplies were planned but found no place in the room
    }
}
