namespace CGD.Level
{
    public enum PropFacing
    {
        Random,
        Square,        // turned a multiple of 90°, lined up with the room
        AgainstWall,   // pushed against a wall (or inner wall) and facing away from it
        TowardCentre,
    }
}
