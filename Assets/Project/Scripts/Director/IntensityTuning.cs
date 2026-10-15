using System;
using UnityEngine;

namespace CGD.Director
{
    // How the player's intensity winds down once the danger has passed.
    [Serializable]
    public class IntensityTuning
    {
        [Tooltip("Seconds after the last stress before intensity starts to fall. It never falls mid-fight")]
        [SerializeField, Min(0f)] private float _decayDelay = 3f;
        [Tooltip("Intensity lost per second once it falls (intensity runs from 0 to 1)")]
        [SerializeField, Min(0f)] private float _decayRate = 0.08f;

        public IntensityTuning() { }

        public IntensityTuning(float decayDelay, float decayRate)
        {
            _decayDelay = decayDelay;
            _decayRate  = decayRate;
        }

        public float DecayDelay => _decayDelay;
        public float DecayRate  => _decayRate;
    }
}
