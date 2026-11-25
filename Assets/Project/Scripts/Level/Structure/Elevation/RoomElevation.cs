namespace CGD.Level
{
    // The floor levels a room can have. Level 0 is the floor every doorway opens onto; each
    // level up or down is one step of StepHeight. A step is kept under a standing eye's height
    // (SightBlocking), so the edge of a platform doesn't hide someone standing below it, but as
    // high as a crouching eye, so it hides someone crouched against it. Neighbouring floor is
    // never more than one level apart, so any height is climbed a level at a time: a tall
    // feature is built in tiers, and an overpass's deck stands OverpassClearance over its floor.
    //
    // Which levels a room may use is set by the build (LevelBuildSettings › Lowest and Highest
    // Level, narrowed per room by RoomStructure.AllowLevels); a level stands only where the
    // room's ceiling leaves Headroom over it (RoomStructure.AllowsLevel). Lowest and Highest are
    // only the bounds the code can store and the Inspector offers, not a design limit.
    public static class RoomElevation
    {
        public const int Lowest = -8;
        public const int Highest = 8;
        public const float StepHeight = 1f;

        // Clear height anyone needs over a floor or a deck, up to the room's ceiling.
        public const float Headroom = 2f;

        // An overpass's deck stands at least this many levels above the floor under it, and is this
        // thick: 1.8 m clear underneath, room for an enemy (1.5 m) and a crouching player, not a standing one.
        public const int   OverpassClearance = 2;
        public const float DeckThickness     = 0.2f;

        // Metres above the room's floor.
        public static float HeightOf(int level) => level * StepHeight;
    }
}
