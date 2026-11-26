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
        Showcase, // in plain view of the doorway a player arrives by, toward the far end: the landmark seen on the way in
        Centred,  // the free spot nearest the room's middle (or what it stands Near), so a count fills a compact, ordered block outward: on a Lattice, rows and columns with no gaps
        Flank,    // in mirrored pairs either side of what it frames (Flanks: what it stands Near, each doorway, each flight of stairs), as close as both fit: planters framing the way in. Count is rounded up to whole pairs
    }
}
