namespace CGD.Enemies
{
    // The kinds of spot an enemy's tactics search for (see EnemyTactics).
    public enum TacticalPlan
    {
        Cover,   // somewhere the obstacle stands between it and its threat
        Flank,   // off to the threat's side, by the least visible route
        Vanish,  // full-height cover nobody can see, for Phase Shift
    }
}
