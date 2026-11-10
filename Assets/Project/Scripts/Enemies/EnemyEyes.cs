namespace CGD.Enemies
{
    // How far above its feet an enemy's eyes are: where it sees, aims and checks lines of sight
    // from — where it stands now, or anywhere it might stand (a cover spot, a vantage, a spawn).
    // One value for every enemy system, so they all agree on what an enemy can see.
    internal static class EnemyEyes
    {
        public const float Height = 1.5f;
    }
}
