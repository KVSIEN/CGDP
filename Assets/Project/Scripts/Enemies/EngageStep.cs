namespace CGD.Enemies
{
    // What an enemy is doing within a fight, for animation, audio and debugging. Reported by
    // whichever brain is in charge; None outside a fight.
    public enum EngageStep
    {
        None,
        Approach,       // closing in on the target
        Strike,         // in range, attacking
        Circle,         // waiting its turn (others are in melee), circling at a distance
        Flank,          // circling round to the target's side
        Hold,           // can't reach the target: holding position, facing it
        MoveToCover,    // running to cover
        FireFromCover,  // shooting from behind cover
        KeepDistance,   // in the open, keeping its preferred range and strafing
    }
}
