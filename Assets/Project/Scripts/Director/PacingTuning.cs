using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Director
{
    // When the pacing cycle moves on (see PacingPhase).
    [Serializable]
    public class PacingTuning
    {
        [Tooltip("Build-up ends, and the peak begins, once intensity reaches this")]
        [SerializeField, Range(0f, 1f)] private float _peakThreshold = 0.75f;
        [Tooltip("Seconds the peak is held, still under pressure, before it fades")]
        [SerializeField, Min(0f)] private float _sustainDuration = 4f;
        [Tooltip("The fade ends, and the calm begins, once intensity has dropped to this")]
        [SerializeField, Range(0f, 1f)] private float _relaxThreshold = 0.25f;
        [Tooltip("Seconds of calm after each peak")]
        [SerializeField] private FloatRange _relaxDuration = new(30f, 45f);
        [Tooltip("Seconds of calm at the start of a session, before the first build-up")]
        [SerializeField, Min(0f)] private float _openingRelax = 15f;

        public PacingTuning() { }

        public PacingTuning(float peakThreshold, float sustainDuration, float relaxThreshold, FloatRange relaxDuration, float openingRelax)
        {
            _peakThreshold   = peakThreshold;
            _sustainDuration = sustainDuration;
            _relaxThreshold  = relaxThreshold;
            _relaxDuration   = relaxDuration;
            _openingRelax    = openingRelax;
        }

        public float      PeakThreshold   => _peakThreshold;
        public float      SustainDuration => _sustainDuration;
        public float      RelaxThreshold  => _relaxThreshold;
        public FloatRange RelaxDuration   => _relaxDuration;
        public float      OpeningRelax    => _openingRelax;
    }
}
