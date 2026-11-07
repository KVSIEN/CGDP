namespace CGD.Enemies
{
    // How a target was last seen moving, judged against the way it was facing.
    public enum TargetMotion
    {
        // Not seen long enough to tell.
        Unknown,
        // Standing still (or as good as).
        Stationary,
        // Going the way it faces: running somewhere.
        Forward,
        // Moving across the way it faces: strafing.
        Sideways,
        // Moving away from the way it faces: backing off while watching.
        Backward,
    }
}
