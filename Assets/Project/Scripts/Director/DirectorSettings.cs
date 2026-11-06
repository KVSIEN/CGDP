using UnityEngine;

namespace CGD.Director
{
    // Tuning for the GameDirector: what stresses the player, how intensity winds down, when
    // the pacing cycle moves on, how the player's performance turns into pressure, and how
    // boldly enemies fight in each phase.
    [CreateAssetMenu(fileName = "DirectorSettings", menuName = "CGD/Director/Director Settings")]
    public class DirectorSettings : ScriptableObject
    {
        [SerializeField] private StressTuning      _stress      = new();
        [SerializeField] private IntensityTuning   _intensity   = new();
        [SerializeField] private PacingTuning      _pacing      = new();
        [SerializeField] private PerformanceTuning _performance = new();
        [SerializeField] private EnemyPacingTuning _enemies     = new();

        public StressTuning      Stress      => _stress;
        public IntensityTuning   Intensity   => _intensity;
        public PacingTuning      Pacing      => _pacing;
        public PerformanceTuning Performance => _performance;
        public EnemyPacingTuning Enemies     => _enemies;
    }
}
