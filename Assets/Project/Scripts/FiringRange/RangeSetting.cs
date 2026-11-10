namespace CGD.FiringRange
{
    // What a RangeSwitch changes about the firing range's dummies.
    public enum RangeSetting
    {
        // Whose defenses the dummies simulate: each faction in turn, or a mix of all.
        Faction,
        // How tough: the faction's tier 1, 2 or 3 enemy.
        Tier,
        // Dummies stand still, sway from side to side, or advance and retreat.
        Movement,
        // How far down the lane the lane dummies stand: the full range, or pulled in closer.
        Distance,
        // Not a setting: stands every dummy back up at full health and clears its effects.
        Reset,
        // The shooter's reserve: normal, or never running out (magazines still need reloading).
        Ammo,
    }
}
