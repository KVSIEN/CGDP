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
        Access    = 1 << 7,   // kept free in front of something to use (a harvest node, a cache)
        Threshold = 1 << 8,   // just inside the way in: where a player arrives and gets the first look (RoomFlowPlanner)
        Goal      = 1 << 9,   // the far end from the way in: where the room's reward and onward doorways lie
        Stairs    = 1 << 10,  // a flight of stairs between two floor levels (RoomStructure.TryAddElevation)
        Landing   = 1 << 11,  // the floor at the top or bottom of a flight, kept clear to walk on and off
        Overpass  = 1 << 12,  // under an overpass's deck: walked under, nothing built or put there (RoomStructure.TryAddOverpass)
    }
}
