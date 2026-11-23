namespace CGD.Enemies
{
    // The metres behind the bands. An enemy's engagement range (EnemyData.EngagementRange) falls
    // into the band whose window holds it; new enemies are set up from `Typical`.
    public static class CombatRanges
    {
        // Under this it is close; from `LongFrom` up it is long; mid between.
        public const float MidFrom  = 8f;
        public const float LongFrom = 18f;

        public static CombatRange Of(float metres) =>
            metres >= LongFrom ? CombatRange.Long : metres >= MidFrom ? CombatRange.Mid : CombatRange.Close;

        // A good distance to hold for the band: the Preferred Range of a ranged enemy of it.
        public static float Typical(CombatRange range) => range switch
        {
            CombatRange.Close => 4f,
            CombatRange.Mid   => 12f,
            _                 => 24f,
        };
    }
}
