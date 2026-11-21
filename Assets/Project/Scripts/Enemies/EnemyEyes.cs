namespace CGD.Enemies
{
    // How far above its feet an enemy's eyes are: where it sees, aims and checks lines of sight
    // from — where it stands now, or anywhere it might stand (a cover spot, a vantage, a spawn).
    // One value for every enemy system, so they all agree on what an enemy can see.
    internal static class EnemyEyes
    {
        public const float Height = 1.5f;

        // Where a line of sight aims at a character's body: its chest, so cover up to the waist
        // doesn't hide it.
        public const float ChestHeight = 1.1f;
    }
}
