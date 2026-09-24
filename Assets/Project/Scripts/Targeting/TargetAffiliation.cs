using System;

namespace CGD.Targeting
{
    // Who a targeting query may pick, relative to the one asking. Combine flags for
    // mixed selections, e.g. Self | Allies for a party heal.
    [Flags]
    public enum TargetAffiliation
    {
        None    = 0,
        Self    = 1 << 0,
        Allies  = 1 << 1,
        Enemies = 1 << 2,
        // Characters without a team (Team.None), or everyone when the asker has no team.
        Neutral = 1 << 3,

        Others   = Allies | Enemies | Neutral,
        Hostiles = Enemies | Neutral,
        Friendly = Self | Allies,
    }
}
