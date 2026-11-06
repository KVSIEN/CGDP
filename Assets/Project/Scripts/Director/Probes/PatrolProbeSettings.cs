using UnityEngine;
using CGD.Core;

namespace CGD.Director
{
    // Tuning for PatrolProbes: how often, during a build-up, a calm enemy already in the level
    // is sent to have a look round near the player. Interval is at neutral pressure; the
    // director's Pressure shortens it.
    [CreateAssetMenu(fileName = "PatrolProbeSettings", menuName = "CGD/Director/Patrol Probe Settings")]
    public class PatrolProbeSettings : ScriptableObject
    {
        [Tooltip("Seconds between probes, counted only during a build-up")]
        [SerializeField] private FloatRange _interval = new(12f, 20f);
        [Tooltip("Probes sent per build-up at most")]
        [SerializeField, Min(0)] private int _perBuildUp = 2;
        [Tooltip("How far from the player (straight line) an enemy may be to be sent: not so close it would stumble straight onto them")]
        [SerializeField] private FloatRange _enemyDistance = new(15f, 45f);
        [Tooltip("How far from the player the spot it checks is — a rough area, never the player's position")]
        [SerializeField] private FloatRange _areaOffset = new(5f, 10f);

        public FloatRange Interval      => _interval;
        public int        PerBuildUp    => _perBuildUp;
        public FloatRange EnemyDistance => _enemyDistance;
        public FloatRange AreaOffset    => _areaOffset;
    }
}
