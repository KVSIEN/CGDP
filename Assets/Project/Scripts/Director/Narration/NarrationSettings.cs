using System;
using UnityEngine;

namespace CGD.Director
{
    // Tuning for DirectorNarration: the beats played as the pacing moves between phases.
    // Several beats may share a phase (each rolls its own chance).
    [CreateAssetMenu(fileName = "NarrationSettings", menuName = "CGD/Director/Narration Settings")]
    public class NarrationSettings : ScriptableObject
    {
        [SerializeField] private NarrationBeat[] _beats = Array.Empty<NarrationBeat>();
        [Tooltip("Metres from the player an enemy may be to shout a beat's bark (so the player hears it)")]
        [SerializeField, Min(1f)] private float _barkRadius = 25f;

        public NarrationBeat[] Beats      => _beats;
        public float           BarkRadius => _barkRadius;
    }
}
