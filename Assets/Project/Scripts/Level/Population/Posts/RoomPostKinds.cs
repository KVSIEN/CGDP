using System;

namespace CGD.Level
{
    // What a floor tile offers an enemy standing on it (RoomPostMap). A tile can be several.
    [Flags]
    public enum RoomPostKinds
    {
        None      = 0,
        Concealed = 1,   // under a concealment prop: hidden until someone is close
        Overwatch = 2,   // on a long open lane with hard cover beside it: a firing position
        Choke     = 4,   // narrow floor: a corridor, aisle or gap between pillars
        DeadEnd   = 8,   // well away from every route: a back corner
        Route     = 16,  // on or beside a walkway: where everyone passes
    }
}
