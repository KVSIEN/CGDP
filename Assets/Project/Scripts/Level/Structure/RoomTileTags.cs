using System;

namespace CGD.Level
{
    // What a room's floor tile is next to or used for. Structure rules and the prop pass
    // read these to keep routes clear and to put things where they make sense.
    [Flags]
    public enum RoomTileTags
    {
        None      = 0,
        Edge      = 1 << 0,   // against a wall
        Corner    = 1 << 1,   // walls on two sides
        Centre    = 1 << 2,   // the spot for the room's centrepiece
        NearDoor  = 1 << 3,   // in front of a doorway
        Walkway   = 1 << 4,   // on the route from a doorway to the centre
        Structure = 1 << 5,   // taken by a pillar or a landmark's furniture
        Prop      = 1 << 6,   // taken by a placed prop
    }
}
