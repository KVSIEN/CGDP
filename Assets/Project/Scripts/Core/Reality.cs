namespace CGD.Core
{
    // The three realities contesting the ship. Tags items, enemies and resources so
    // drops, gear and encounters can be filtered and themed by where they came from.
    // Neutral is the default so an unassigned asset never silently claims a reality.
    public enum Reality
    {
        Neutral = 0,
        Tech    = 1,
        Bio     = 2,
        Void    = 3,
    }
}
