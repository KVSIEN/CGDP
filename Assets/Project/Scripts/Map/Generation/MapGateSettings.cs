using System;
using UnityEngine;

namespace CGD.Map
{
    // Locked and secret entrances. Only an optional area with a single way in can be
    // gated, so a gate can never be walked around.
    [Serializable]
    public class MapGateSettings
    {
        [Tooltip("Chance an optional area's entrance is Locked")]
        [SerializeField, Range(0f, 1f)] private float _lockedChance = 0.15f;
        [Tooltip("Chance an optional area's entrance is Secret (rolled only when not Locked)")]
        [SerializeField, Range(0f, 1f)] private float _secretChance = 0.15f;
        [SerializeField, Min(0)] private int _maxGates = 2;

        public float LockedChance => _lockedChance;
        public float SecretChance => _secretChance;
        public int   MaxGates     => _maxGates;
    }
}
