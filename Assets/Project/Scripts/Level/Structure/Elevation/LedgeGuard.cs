namespace CGD.Level
{
    // What stands along a ledge, on its higher side. Either guard stands RoomElevation.GuardHeight
    // over the higher floor, so from below it tops out above Mantle Max Height: a guarded ledge is
    // crossed only by its stairs. An open one is a single level, mantled up and dropped off.
    public enum LedgeGuard
    {
        None,      // an open drop, mantled up and dropped off
        Railing,   // a see-through rail: stops crossing, not sight or shots, and gives no cover
        HalfWall,  // a solid parapet: stops crossing and is low cover on both sides of it
    }
}
