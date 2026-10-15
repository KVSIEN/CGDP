namespace CGD.Level
{
    // How a placement chooses among the tiles that suit it.
    public enum PropSelection
    {
        Random,   // any suitable tile
        Spread,   // the tile farthest from what is already placed: even, natural spacing instead of clumps and gaps
        Cover,    // the tile that breaks the longest open sightline: cover that cuts the room's firing lanes
        Cluster,  // next to what is already placed, with the odd new patch: thickets, drifts and heaps instead of even spacing
        Line,     // along what is already placed, down the room's long axis: machines nose to tail in rows
    }
}
