namespace CGD.Enemies
{
    // A component on a summoned enemy that wants to know who called it up (EnemyMinions tells
    // it as it spawns): an echo reporting back to the Void that left it.
    internal interface ISummoned
    {
        void SummonedBy(EnemyAI summoner);
    }
}
