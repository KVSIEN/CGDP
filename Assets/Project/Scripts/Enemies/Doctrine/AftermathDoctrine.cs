using System;
using UnityEngine;

namespace CGD.Enemies
{
    // What a faction does when a fight is over, so awareness does not simply switch off: how
    // long it stays on edge, and how it stands over what it killed — or feeds on it. The
    // defaults leave a won fight as it was: no edge, a plain pause.
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

        [Header("Feeding")]
        [Tooltip("After a won fight it feeds on what it killed (never its kin or the player) instead of standing over it")]
        [SerializeField] private bool _feeds;
        [Tooltip("Seconds it feeds, if the body lasts that long")]
        [SerializeField, Min(0f)] private float _feedSeconds = 4f;
        [Tooltip("Share of its full health it heals each second it feeds")]
        [SerializeField, Range(0f, 1f)] private float _feedHealPerSecond = 0.06f;
        [Tooltip("How much it notices while feeding, as a share of usual (sight, hearing, smell; never touch)")]
        [SerializeField, Range(0f, 1f)] private float _feedAttention = 0.35f;

        public float VictoryPauseScale   => _victoryPauseScale;
        public GazePattern VictoryGaze   => _victoryGaze;
        public bool  Feeds               => _feeds;
        public float FeedSeconds         => _feedSeconds;
        public float FeedHealPerSecond   => _feedHealPerSecond;
        public float FeedAttention       => _feedAttention;

        public float GiveUpCaution(float profileDuration) => profileDuration * _cautionScale;
        public float VictoryCaution(float profileDuration) => profileDuration * _cautionScale * _victoryCautionShare;
    }
}
