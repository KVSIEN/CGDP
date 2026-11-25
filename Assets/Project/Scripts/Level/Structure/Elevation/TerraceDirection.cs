namespace CGD.Level
{
    // Which way a terraced room's lanes run.
    public enum TerraceDirection
    {
        AlongLength,   // the length of the room, side by side across it: a promenade with raised walks along its walls
        AcrossLength,  // across the room, one after another down its length: a hall stepping down toward a stage
        Either,        // whichever the doorways allow, tried in a random order
    }
}
