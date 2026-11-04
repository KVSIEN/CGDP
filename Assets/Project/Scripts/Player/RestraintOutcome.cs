namespace CGD.Player
{
    public enum RestraintOutcome
    {
        // Still restrained (or not restrained at all).
        None,
        // The player mashed through it.
        BrokeFree,
        // The restraint ran out of time and let go.
        TimedOut,
    }
}
