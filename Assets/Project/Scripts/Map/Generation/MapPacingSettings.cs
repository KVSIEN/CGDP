using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace CGD.Map
{
    // Rhythm along the main path, on top of the intensity curve: no endless chains of
    // fights, and a breather after a tier-3 fight. Per-type spacing lives on MapNodeTypeRule.
    [Serializable]
    public class MapPacingSettings
    {
        [Tooltip("Room types that count as a fight")]
        [SerializeField] private MapNodeType[] _combatTypes = { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Breach, MapNodeType.Lockdown, MapNodeType.Holdout, MapNodeType.Rift };
        [Tooltip("Most fights in a row on the main path. 0 = no limit")]
        [SerializeField, Min(0)] private int _maxCombatInARow = 3;
        [Tooltip("The main path room right after a tier-3 fight is never a fight (rooms are kept below tier 3 otherwise)")]
        [FormerlySerializedAs("_restAfterElite")]
        [SerializeField] private bool _restAfterTopTier = true;
        [Tooltip("Used for a room no rule can take without breaking the pacing")]
        [SerializeField] private MapNodeType _restType = MapNodeType.Event;

        public int         MaxCombatInARow => _maxCombatInARow;
        public bool        RestAfterTopTier => _restAfterTopTier;
        public MapNodeType RestType        => _restType;

        public bool IsCombat(MapNodeType type) => Array.IndexOf(_combatTypes, type) >= 0;
    }
}
