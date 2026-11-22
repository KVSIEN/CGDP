namespace CGD.Enemies
{
    // Where a Lay Trap node puts its trap.
    public enum TrapPlacement
    {
        // Between it and where it believes its target is, a few metres out: rushing the trapper
        // means crossing it.
        InTheWay,
        // Right where it believes its target is (or went): a mine on the spot it lost you.
        WhereTargetIs,
    }
}
