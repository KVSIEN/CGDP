namespace CGD.UI
{
    // Every number that decides how damage numbers feel, in one place. Values are screen
    // pixels and seconds unless noted, so the same tuning reads identically at any range —
    // a number 40 m out is spaced and moves exactly like one at 3 m.
    public static class DamageNumberStyle
    {
        // Hard ceiling on live numbers. A held LMG trigger or a grenade in a crowd can outrun
        // any fade time, so the oldest are retired early to keep the screen legible.
        public const int MaxActive = 28;
        // How far below the cap the oldest number starts fading, so the cap is normally reached
        // by numbers finishing gracefully rather than being cut off mid-life.
        public const int RetireHeadroom = 6;

        // ── Placement ─────────────────────────────────────────────────────────
        // Hits landing within this many metres of each other count as one group, which is what
        // keeps a shotgun blast to one tidy cluster instead of ten numbers on a single point.
        public const float ClusterRadius = 1.6f;
        // Successive hits in a group step out sideways in a fixed alternating order — no
        // randomness anywhere, so the same burst always builds the same recognisable shape.
        public const float ColumnStepPx  = 14f;
        public const int   ColumnSlots   = 5;   // pattern repeats after this many
        public const float RowStepPx     = 7f; // slight lift per hit, so repeats never land exactly on each other
        public const int   RowStepMax    = 6;   // rows stop climbing after this, keeping the group compact

        // ── Motion ────────────────────────────────────────────────────────────
        // A toss: thrown up out of the impact, slowed by gravity, with a small sideways push
        // away from the middle of the group. Keep SideSpeedPx well under ColumnStepPx or the
        // spread it adds over a number's life washes out the pattern the group was placed in.
        public const float RiseSpeedPx  = 170f;
        public const float SideSpeedPx  = 8f;
        public const float GravityPx    = 200f;

        // ── Life ──────────────────────────────────────────────────────────────
        public const float HoldSeconds   = 0.45f;
        public const float FadeSeconds   = 0.30f;
        public const float CritHoldBonus = 0.20f;

        // ── Punch ─────────────────────────────────────────────────────────────
        public const float PunchFromScale = 0.55f; // scale a number pops in from
        public const float PunchPeakScale = 1.15f; // overshoot before settling to 1
        public const float PunchRise      = 0.07f;
        public const float PunchSettle    = 0.13f;
        public const float CritPunchBonus = 0.12f; // added to the overshoot on a critical hit
        // A new number joining a group bumps the ones already there, so a burst reads as damage
        // piling up rather than as unrelated numbers that happen to share a spot.
        public const float KickScale      = 0.14f;
        public const float KickSeconds    = 0.12f;

        // ── Size ──────────────────────────────────────────────────────────────
        // Rendered height in pixels, interpolated by damage so a chip and a heavy hit read
        // apart at a glance. Damage at or above SizeRefDamage gets the full size.
        public const float MinHeightPx   = 38f;
        public const float MaxHeightPx   = 48f;
        public const float SizeRefDamage = 60f;
        public const float CritSizeScale = 1.1f;
    }
}
