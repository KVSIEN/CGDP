using System;
using UnityEngine;

namespace CGD.Director
{
    // How the director reads the player's performance into pressure (see SessionPerformance).
    [Serializable]
    public class PerformanceTuning
    {
        [Tooltip("Pressure for a player who keeps struggling: fewer, rarer reinforcements and longer calms")]
        [SerializeField, Range(0.1f, 1f)] private float _minPressure = 0.7f;
        [Tooltip("Pressure for a player who keeps cruising: more, sooner reinforcements and shorter calms")]
        [SerializeField, Range(1f, 3f)] private float _maxPressure = 1.35f;
        [Tooltip("A peak the player came out of with at least this share of health counts as easy")]
        [SerializeField, Range(0f, 1f)] private float _comfortableHealth = 0.6f;
        [Tooltip("A peak that took the player down to this share of health or less counts as hard")]
        [SerializeField, Range(0f, 1f)] private float _strugglingHealth = 0.3f;
        [Tooltip("How far one easy or hard peak moves the rating (the rating runs from -1 to 1)")]
        [SerializeField, Range(0f, 1f)] private float _step = 0.2f;
        [Tooltip("How far a death moves the rating down")]
        [SerializeField, Range(0f, 2f)] private float _deathStep = 0.5f;

        public PerformanceTuning() { }

        public PerformanceTuning(float minPressure, float maxPressure, float comfortableHealth, float strugglingHealth, float step, float deathStep)
        {
            _minPressure       = minPressure;
            _maxPressure       = maxPressure;
            _comfortableHealth = comfortableHealth;
            _strugglingHealth  = strugglingHealth;
            _step              = step;
            _deathStep         = deathStep;
        }

        public float MinPressure       => _minPressure;
        public float MaxPressure       => _maxPressure;
        public float ComfortableHealth => _comfortableHealth;
        public float StrugglingHealth  => _strugglingHealth;
        public float Step              => _step;
        public float DeathStep         => _deathStep;
    }
}
