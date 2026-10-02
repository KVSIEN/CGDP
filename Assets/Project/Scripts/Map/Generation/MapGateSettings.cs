using System;
using UnityEngine;

namespace CGD.Map
{
    // Locked and secret entrances. Only an optional area with a single way in can be
    // gated, so a gate can never be walked around. Every Locked gate gets a key in a room
    // the player can reach without opening any gate.
    [Serializable]
    public class MapGateSettings
    {
        [Tooltip("Chance an optional area's entrance is Locked")]
        [SerializeField, Range(0f, 1f)] private float _lockedChance = 0.15f;
        [Tooltip("Chance an optional area's entrance is Secret (rolled only when not Locked)")]
        [SerializeField, Range(0f, 1f)] private float _secretChance = 0.15f;
        [SerializeField, Min(0)] private int _maxGates = 2;
        [Tooltip("Rooms from Start before the first gate may appear. 0 = even off the Start room")]
        [SerializeField, Min(0)] private int _minDepth;

        [Header("Keys")]
        [Tooltip("Keys sit no deeper than their gate, so the key is found before (or on the way to) its door rather than past it")]
        [SerializeField] private bool _keyBeforeGate = true;
        [Tooltip("Keys prefer rooms off the main path, so opening a locked area means exploring first")]
        [SerializeField] private bool _keyOffMainPath = true;

        public float LockedChance   => _lockedChance;
        public float SecretChance   => _secretChance;
        public int   MaxGates       => _maxGates;
        public int   MinDepth       => _minDepth;
        public bool  KeyBeforeGate  => _keyBeforeGate;
        public bool  KeyOffMainPath => _keyOffMainPath;
        public bool  Enabled        => _maxGates > 0 && (_lockedChance > 0f || _secretChance > 0f);
    }
}
