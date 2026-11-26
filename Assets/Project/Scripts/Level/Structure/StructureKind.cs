namespace CGD.Level
{
    // The kinds of structure a room is built of, in the order they count for: full walls shape
    // a room's flow most, then buildings (huts standing in it), then pillars, then half walls.
    public enum StructureKind
    {
        Wall,
        Building,
        Pillar,
        HalfWall,
    }
}
