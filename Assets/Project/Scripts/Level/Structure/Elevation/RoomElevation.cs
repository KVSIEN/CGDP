namespace CGD.Level
{
    // The floor levels a room can have. Level 0 is the floor every doorway opens onto; each
    // level up or down is one step of StepHeight. A step is kept under eye height (SightBlocking),
    // so the edge of a platform doesn't hide someone standing below it.
    public static class RoomElevation
    {
        public const int Lowest = -1;
        public const int Highest = 1;
        public const float StepHeight = 1f;

        // Metres above the room's floor.
        public static float HeightOf(int level) => level * StepHeight;
    }
}
