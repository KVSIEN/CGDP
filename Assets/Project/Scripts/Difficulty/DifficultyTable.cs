using UnityEngine;

namespace CGD.Difficulty
{
    // The tuning for every difficulty level. One shared asset, referenced by GameFlowSettings.
    [CreateAssetMenu(fileName = "DifficultyTable", menuName = "CGD/Difficulty/Difficulty Table")]
    public class DifficultyTable : ScriptableObject
    {
        [SerializeField] private DifficultyTuning _easy   = new(0.75f, 0.7f);
        [SerializeField] private DifficultyTuning _normal = new(1f, 1f);
        [SerializeField] private DifficultyTuning _hard   = new(1.35f, 1.3f);

        public DifficultyTuning For(DifficultyLevel level) => level switch
        {
            DifficultyLevel.Easy => _easy,
            DifficultyLevel.Hard => _hard,
            _                    => _normal,
        };
    }
}
