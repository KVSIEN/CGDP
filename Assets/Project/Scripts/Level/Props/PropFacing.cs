namespace CGD.Level
{
    // Every facing but Random keeps a prop square to the room (a multiple of 90°).
    public enum PropFacing
    {
        Random,        // any angle for a single tile: loose clutter only
        Square,        // turned a multiple of 90°, lined up with the room
        AgainstWall,   // pushed against a wall (or inner wall) and facing away from it
        TowardCentre,
        AcrossSightline, // front toward the longest open stretch of floor through it, so a barrier faces the lane it blocks
        AlongRoom,       // length (Z) running down the room's longer axis, front toward the room's middle: rows of machines, lines of desks
        TowardNear,      // front toward the nearest of what it stands Near: loungers facing the pool, chairs facing the stage (Square without a Near)
        Diagonal,        // a quarter turn and 45° more, for a single tile: café tables turned on the diagonal, a herringbone of crates
        AgainstDiagonal, // beside a 45° wall (DiagonalWall), single tile, facing straight out from it: a planter or console tucked against an angled wall
    }
}
