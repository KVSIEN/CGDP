using System;
using UnityEngine;

namespace CGD.Level
{
    // How strongly a faction's design shows in a room, from how firmly the map holds it (the
    // faction painter's influence: 1 at a territory's origin, less each room out). Core rooms
    // get the design at full strength, frontier rooms a thinner version with the ship showing
    // through (FactionStyle). Set on LevelBuildSettings › Territory Depth.
    [Serializable]
    public class TerritoryDepth
    {
        [Tooltip("Influence from which a room is core: its faction's design at full strength")]
        [SerializeField, Range(0f, 1f)] private float _coreInfluence = 0.7f;
        [Tooltip("Influence at and below which a room is frontier")]
        [SerializeField, Range(0f, 1f)] private float _frontierInfluence = 0.35f;
        [Tooltip("How strongly the design shows on the frontier (1 = everywhere the same)")]
        [SerializeField, Range(0f, 1f)] private float _frontierStrength = 0.4f;

        public TerritoryDepth() { }

        public TerritoryDepth(float coreInfluence, float frontierInfluence, float frontierStrength)
        {
            _coreInfluence     = coreInfluence;
            _frontierInfluence = frontierInfluence;
            _frontierStrength  = frontierStrength;
        }

        // 0 influence means none was painted (an old graph, a made-up room): full strength.
        public float StrengthOf(float influence)
        {
            if (influence <= 0f) return 1f;
            if (_coreInfluence <= _frontierInfluence) return influence >= _coreInfluence ? 1f : _frontierStrength;
            return Mathf.Lerp(_frontierStrength, 1f, Mathf.InverseLerp(_frontierInfluence, _coreInfluence, influence));
        }

        public string NameOf(float strength) =>
            strength >= 1f ? "core" : strength <= _frontierStrength ? "frontier" : "held";
    }
}
