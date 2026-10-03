namespace CGD.Map
{
    // What the player experiences at a node. Start, Boss and Exit are structural:
    // the generator places them itself, so type rules never produce them. New types go at
    // the end: assets store these as numbers.
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
        Exit,
        Resupply,   // a calm stop with an ammo cache before more fighting
        Breach,     // a fight where two realities overlap: both factions' enemies
    }
}
