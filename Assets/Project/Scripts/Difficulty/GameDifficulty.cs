using UnityEngine;
using CGD.Settings;

namespace CGD.Difficulty
{
    // The difficulty in force: the level the player chose (GameSettings) looked up in the
    // table GameFlow registers. Systems ask for Current when they need a number, so a change
    // in the settings menu applies to everything read afterwards. Without a table every
    // level plays as balanced.
    public static class GameDifficulty
    {
        public static DifficultyTable Table { get; set; }

        public static DifficultyTuning Current => For(GameSettings.Current.Difficulty);

        public static DifficultyTuning For(DifficultyLevel level) =>
            Table != null ? Table.For(level) : DifficultyTuning.None;

        public static float EnemyHealth => Current.EnemyHealth;
        public static float DamageTaken => Current.DamageTaken;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Table = null;
    }
}
