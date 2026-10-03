using System;
using UnityEngine;

namespace CGD.Map
{
    // Rhythm along the main path, on top of the intensity curve: no endless chains of
    // fights, and a breather after an Elite. Per-type spacing lives on MapNodeTypeRule.
    [Serializable]
    public class MapPacingSettings
    {
        [Tooltip("Room types that count as a fight")]
        [SerializeField] private MapNodeType[] _combatTypes = { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Breach, MapNodeType.Lockdown, MapNodeType.Holdout, MapNodeType.Rift };
        [Tooltip("Most fights in a row on the main path. 0 = no limit")]
        [SerializeField, Min(0)] private int _maxCombatInARow = 3;
        [Tooltip("The main path room right after an Elite is never a fight")]
        [SerializeField] private bool _restAfterElite = true;
        [Tooltip("Used for a room no rule can take without breaking the pacing")]
        [SerializeField] private MapNodeType _restType = MapNodeType.Event;

        public int         MaxCombatInARow => _maxCombatInARow;
        public bool        RestAfterElite  => _restAfterElite;
        public MapNodeType RestType        => _restType;

        public bool IsCombat(MapNodeType type) => Array.IndexOf(_combatTypes, type) >= 0;
    }
}
