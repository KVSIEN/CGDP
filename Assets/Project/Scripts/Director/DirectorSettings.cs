using UnityEngine;

namespace CGD.Director
{
    // Tuning for the GameDirector: what stresses the player, how intensity winds down, when
    // the pacing cycle moves on and how the player's performance turns into pressure.
    [CreateAssetMenu(fileName = "DirectorSettings", menuName = "CGD/Director/Director Settings")]
    public class DirectorSettings : ScriptableObject
    {
        [SerializeField] private StressTuning      _stress      = new();
        [SerializeField] private IntensityTuning   _intensity   = new();
        [SerializeField] private PacingTuning      _pacing      = new();
        [SerializeField] private PerformanceTuning _performance = new();

        public StressTuning      Stress      => _stress;
        public IntensityTuning   Intensity   => _intensity;
        public PacingTuning      Pacing      => _pacing;
        public PerformanceTuning Performance => _performance;
    }
}
