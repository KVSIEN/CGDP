using System;

namespace CGD.Level
{
    // Which readings of a room use what can be seen in every direction (RoomVisibility) in place
    // of the open run along each row and column (SightlineMap). Off by default, one switch for each
    // reader so they can be tried apart; LevelBuildSettings holds the choice. The traits steer
    // generation and the Room Lab checks them, so the two always read the room the same way.
    [Flags]
    public enum IsovistUse
    {
        None   = 0,
        // Walls and lines of fire (RoomTraitMeasure): walls are the shut-in floor, lines of fire the
        // longest line of sight in any direction with cover beside it.
        Traits = 1 << 0,
        // Where enemies stand (RoomPostMap): a choke is a shut-in tile, an overwatch a tile with
        // a long sightline and cover beside it.
        Posts  = 1 << 1,
        // Where cover goes (PropSelection.Cover): the tile with the longest sightline.
        Cover  = 1 << 2,
    }
}
