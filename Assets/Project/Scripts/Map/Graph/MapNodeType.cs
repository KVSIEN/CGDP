namespace CGD.Map
{
    // What the player experiences at a node. Start, Boss and Exit are structural:
    // the generator places them itself, so type rules never produce them.
    public enum MapNodeType
    {
        Start,
        Combat,
        Elite,
        Puzzle,
        Shop,
        Event,
        Treasure,
        Boss,
        Exit
    }
}
