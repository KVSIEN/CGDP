using System;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Extra links that turn the map from a tree into a web. None ever touches the Boss or
    // Exit, or brings the Boss closer to Start than Min Boss Depth.
    [Serializable]
    public class MapLoopSettings
    {
        [Tooltip("Links between neighbouring rooms that aren't connected yet")]
        [SerializeField] private IntRange _loopCount = new(1, 2);
        [Tooltip("Shortcut links that jump from a shallow room to a deeper one nearby")]
        [SerializeField] private IntRange _shortcutCount = new(0, 1);
        [Tooltip("Furthest apart a shortcut's two rooms may be, in grid steps")]
        [SerializeField, Min(1)] private int _shortcutReach = 2;
        [Tooltip("Fewest rooms a shortcut must skip")]
        [SerializeField, Min(1)] private int _shortcutMinSkip = 1;
        [Tooltip("Chance a shortcut is one-way: barred until opened from its deep end, so it's a way back rather than a way ahead")]
        [SerializeField, Range(0f, 1f)] private float _oneWayShortcutChance = 0.5f;

        public IntRange LoopCount       => _loopCount;
        public IntRange ShortcutCount   => _shortcutCount;
        public int      ShortcutReach   => _shortcutReach;
        public int      ShortcutMinSkip => _shortcutMinSkip;
        public float    OneWayShortcutChance => _oneWayShortcutChance;
    }
}
