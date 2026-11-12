namespace CGD.Enemies
{
    // The level-wide side of enemy group behaviour — encounters (who fights alongside whom,
    // with which role) and faction overseers (each faction's own director) — ticked by the
    // enemies themselves, so a scene needs nothing extra placed. Each keeps its own rate.
    internal static class EnemyCoordination
    {
        public static void Tick(float time)
        {
            Overseers.Tick(time);
            EncounterBoard.Tick(time);
        }
    }
}
