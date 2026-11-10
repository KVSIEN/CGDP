namespace CGD.Map
{
    // Which way the main path may leave Start.
    public enum MapPathDirection
    {
        // Left to right like a timeline: never steps back toward Start's column, so the
        // Boss is always east of Start and Max Spread limits the rows above and below it.
        East,
        // Any direction: each step moves one cell further from Start, so the path can set
        // off north, south, east or west and Start can sit in the middle of the map. Max
        // Spread limits how far rooms stray from the main path.
        Any,
    }
}
