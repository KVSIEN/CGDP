namespace CGD.Level
{
    // How the room tiers of an objective's steps relate, on top of each step's own range.
    public enum ObjectiveTierOrder
    {
        Any,         // each step rolls its own tier (3-1-2 is fine)
        NeverDown,   // each step at least as tough as the one before (1-1-3, 1-2-2, 2-2-3…)
        Same,        // every step in rooms of one tier (1-1-1 or 3-3-3)
    }
}
