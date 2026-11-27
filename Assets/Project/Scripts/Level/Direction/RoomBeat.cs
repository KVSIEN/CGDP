namespace CGD.Level
{
    // The part a room plays in the level's pacing, given by the map director (MapDirector) from
    // the map graph before the level is built. Tension rises through Build rooms to a Peak, eases
    // in the Relief after it and rests in the calm rooms between; the room leading into the boss
    // is the Approach, a last chance to restock, and the boss room the Climax.
    // Stored as numbers in assets: new beats go at the end.
    public enum RoomBeat
    {
        None,       // not directed (no director, or a room the graph doesn't reach)
        Rest,       // a calm room: no fight is planned here
        Build,      // an ordinary fight, on the way up to the next peak
        Peak,       // the hardest fight of its stretch of map: a big arena with plenty of cover
        Relief,     // the fight straight after a peak: smaller, fewer enemies, time to breathe
        Approach,   // the fight leading into the boss: lighter, with supplies put down
        Climax,     // the boss room
    }
}
