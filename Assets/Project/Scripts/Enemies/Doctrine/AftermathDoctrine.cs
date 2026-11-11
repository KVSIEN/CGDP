using System;
using UnityEngine;

namespace CGD.Enemies
{
    // What a faction does when a fight is over, so awareness does not simply switch off: how
    // long it stays on edge, and how it stands over what it killed. The defaults leave a won
    // fight as it was: no edge, a plain pause.
    [Serializable]
    public sealed class AftermathDoctrine
    {
        [Tooltip("Scales the profile's time on edge after a search that found nothing")]
        [SerializeField, Min(0f)] private float _cautionScale = 1f;
        [Tooltip("Share of that time it stays on edge after winning a fight. 0 = it relaxes at once")]
        [SerializeField, Range(0f, 1f)] private float _victoryCautionShare;
        [Tooltip("Scales the profile's pause over what it killed")]
        [SerializeField, Min(0f)] private float _victoryPauseScale = 1f;
        [Tooltip("How it spends that pause: Hold stares at the body, Sweep scans round it, Scatter sniffs about it")]
        [SerializeField] private GazePattern _victoryGaze = GazePattern.Hold;

        public float VictoryPauseScale   => _victoryPauseScale;
        public GazePattern VictoryGaze   => _victoryGaze;

        public float GiveUpCaution(float profileDuration) => profileDuration * _cautionScale;
        public float VictoryCaution(float profileDuration) => profileDuration * _cautionScale * _victoryCautionShare;
    }
}
