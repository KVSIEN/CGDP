using System;
using UnityEngine;

namespace CGD.Director
{
    // One stem of the director's adaptive score. All stems loop in sync and each is faded in
    // by the player's intensity (vertical layering): a calm bed always under, tension
    // creeping in as pressure builds, percussion once the fight is on, a peak layer only
    // while the peak is held.
    [Serializable]
    public class MusicLayer
    {
        [SerializeField] private AudioClip _clip;
        [Tooltip("Intensity at which the layer starts to fade in")]
        [SerializeField, Range(0f, 1f)] private float _fadeInFrom;
        [Tooltip("Intensity at which the layer is at full volume")]
        [SerializeField, Range(0f, 1f)] private float _fadeInTo;
        [Tooltip("Only heard while the director holds the peak")]
        [SerializeField] private bool _peakOnly;
        [Tooltip("Silent during the calm, whatever the intensity — lets a fight's music drop out once the director lets go")]
        [SerializeField] private bool _silentInCalm;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        public MusicLayer() { }

        public MusicLayer(float fadeInFrom, float fadeInTo, bool peakOnly = false, bool silentInCalm = false, float volume = 1f)
        {
            _fadeInFrom   = fadeInFrom;
            _fadeInTo     = fadeInTo;
            _peakOnly     = peakOnly;
            _silentInCalm = silentInCalm;
            _volume       = volume;
        }

        public AudioClip Clip => _clip;

        // The volume the layer heads for (before the player's music volume).
        public float TargetVolume(PacingPhase phase, float intensity)
        {
            if (_peakOnly && phase != PacingPhase.SustainPeak) return 0f;
            if (_silentInCalm && phase == PacingPhase.Relax) return 0f;
            return FadeIn(intensity) * _volume;
        }

        private float FadeIn(float intensity)
        {
            if (_fadeInTo <= _fadeInFrom) return intensity >= _fadeInFrom ? 1f : 0f;
            return Math.Clamp((intensity - _fadeInFrom) / (_fadeInTo - _fadeInFrom), 0f, 1f);
        }
    }
}
