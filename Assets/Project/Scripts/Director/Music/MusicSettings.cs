using System;
using UnityEngine;

namespace CGD.Director
{
    // The director's adaptive score: layered stems faded by intensity, plus one-shot stingers
    // marking the peak and the calm after it. Layers without a clip are skipped.
    [CreateAssetMenu(fileName = "MusicSettings", menuName = "CGD/Director/Music Settings")]
    public class MusicSettings : ScriptableObject
    {
        [Tooltip("Stems of one piece, all the same length — they loop together")]
        [SerializeField] private MusicLayer[] _layers = Array.Empty<MusicLayer>();
        [Tooltip("How fast a layer's volume moves toward its target, per second (1 = silent to full in a second)")]
        [SerializeField, Min(0.01f)] private float _fadeSpeed = 0.5f;

        [Header("Stingers")]
        [Tooltip("Played as the director reaches the peak. Optional")]
        [SerializeField] private AudioClip _peakStinger;
        [Tooltip("Played as a calm begins after a peak. Optional")]
        [SerializeField] private AudioClip _calmStinger;
        [SerializeField, Range(0f, 1f)] private float _stingerVolume = 1f;

        public MusicLayer[] Layers        => _layers;
        public float        FadeSpeed     => _fadeSpeed;
        public float        StingerVolume => _stingerVolume;

        public AudioClip StingerFor(PacingPhase phase) => phase switch
        {
            PacingPhase.SustainPeak => _peakStinger,
            PacingPhase.Relax       => _calmStinger,
            _                       => null,
        };
    }
}
