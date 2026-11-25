namespace CGD.Editor
{
    // What the Room Lab paints over the plan: one generation pass's view of the floor at a time.
    public enum RoomLabLayer
    {
        Plan,        // just the blueprint: floor, walls, pillars, furniture
        Tags,        // what each tile is for: walkways, doorway fronts, kept-clear access, edges, corners
        Zones,       // the areas inner walls split the room into
        Sightlines,  // how far you can see along each tile's row or column (red = long, exposed)
        Posts,       // what each tile offers an enemy, where the room's enemies start and the loops they patrol
        Flow,        // the way in, the threshold just inside it, the contest and the goal at the far end; whether a flank goes round
        Elevation,   // raised and sunken floor, the stairs between levels and the ledges you can drop off
    }
}
