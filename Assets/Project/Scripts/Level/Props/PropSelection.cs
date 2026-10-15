namespace CGD.Level
{
    // How a placement chooses among the tiles that suit it.
    public enum PropSelection
    {
        Random,   // any suitable tile
        Spread,   // the tile farthest from what is already placed: even, natural spacing instead of clumps and gaps
        Cover,    // the tile that breaks the longest open sightline: cover that cuts the room's firing lanes
    }
}
