namespace CGD.Enemies
{
    // Who leads an encounter, and who steps up when the lead falls.
    public enum SuccessionRule
    {
        // Chain of command: the highest tier, then whoever has been in the fight longest.
        Rank,
        // Survival: the strongest right now, tier and health together, so a fresh tier 2 can
        // take over from a badly hurt tier 3.
        Strength,
        // Depth: the highest tier, then whoever has the most of its shield (its ward) left.
        Depth,
    }
}
