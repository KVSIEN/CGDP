using UnityEngine;

namespace CGD.Director
{
    // Tuning for the GameDirector: what stresses the player, how intensity winds down and
    // when the pacing cycle moves on.
    [CreateAssetMenu(fileName = "DirectorSettings", menuName = "CGD/Director/Director Settings")]
    public class DirectorSettings : ScriptableObject
    {
        [SerializeField] private StressTuning    _stress    = new();
        [SerializeField] private IntensityTuning _intensity = new();
        [SerializeField] private PacingTuning    _pacing    = new();

        public StressTuning    Stress    => _stress;
        public IntensityTuning Intensity => _intensity;
        public PacingTuning    Pacing    => _pacing;
    }
}
