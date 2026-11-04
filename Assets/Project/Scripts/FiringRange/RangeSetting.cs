namespace CGD.FiringRange
{
    // What a RangeSwitch changes about the firing range's dummies.
    public enum RangeSetting
    {
        // Whose defenses the dummies simulate: each faction in turn, or a mix of all.
        Faction,
        // How tough: the faction's tier 1, 2 or 3 enemy.
        Tier,
        // Dummies sway from side to side, or stand still.
        Movement,
    }
}
