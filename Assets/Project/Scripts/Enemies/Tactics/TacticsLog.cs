namespace CGD.Enemies
{
    // An enemy's latest search of each kind — why it went to that crate, flanked that way or
    // reappeared there. Read by the spectator's overlays and inspector; nothing decides by it.
    public sealed class TacticsLog
    {
        public TacticalDecision Cover  { get; } = new(TacticalPlan.Cover);
        public TacticalDecision Flank  { get; } = new(TacticalPlan.Flank);
        public TacticalDecision Vanish { get; } = new(TacticalPlan.Vanish);

        public TacticalDecision For(TacticalPlan plan) => plan switch
        {
            TacticalPlan.Flank  => Flank,
            TacticalPlan.Vanish => Vanish,
            _                   => Cover,
        };
    }
}
