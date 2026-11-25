namespace CGD.Level
{
    // The floor levels a room can have. Level 0 is the floor every doorway opens onto; each
    // level up or down is one step of StepHeight. A step is kept under a standing eye's height
    // (SightBlocking), so the edge of a platform doesn't hide someone standing below it, but as
    // high as a crouching eye, so it hides someone crouched against it. Level 2 is for
    // overpasses: a deck two levels above the floor it crosses.
    public static class RoomElevation
    {
        public const int Lowest = -1;
        public const int Highest = 2;
        public const float StepHeight = 1f;

        // An overpass's deck stands at least this many levels above the floor under it, and is this
        // thick: 1.8 m clear underneath, room for an enemy (1.5 m) and a crouching player, not a standing one.
        public const int   OverpassClearance = 2;
        public const float DeckThickness     = 0.2f;
        // Clear height a deck needs above it, up to the room's ceiling.
        public const float DeckHeadroom      = 2f;

        // Metres above the room's floor.
        public static float HeightOf(int level) => level * StepHeight;
    }
}
