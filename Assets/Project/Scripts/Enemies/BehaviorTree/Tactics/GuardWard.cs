namespace CGD.Enemies
{
    // Which ally a Guard Ally keeps by.
    public enum GuardWard
    {
        // The highest tier ally near it (a boss, a broodmother, an engineer): a bodyguard.
        HighestTier,
        // The ally nearest the threat, the one leading the hunt: a pack follower running with
        // its predator.
        LeadHunter,
    }
}
