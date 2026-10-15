namespace CGD.Level
{
    public enum PropFacing
    {
        Random,
        Square,        // turned a multiple of 90°, lined up with the room
        AgainstWall,   // pushed against a wall (or inner wall) and facing away from it
        TowardCentre,
        AcrossSightline, // front toward the longest open stretch of floor through it, so a barrier faces the lane it blocks
    }
}
