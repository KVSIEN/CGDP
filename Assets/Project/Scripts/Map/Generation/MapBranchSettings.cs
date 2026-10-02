using System;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Side areas off the main path. Branches are added until the map reaches its room
    // count, so Length sets the character: many short side rooms or a few long wings.
    [Serializable]
    public class MapBranchSettings
    {
        [Tooltip("Rooms per branch. A branch boxed in by other rooms ends early")]
        [SerializeField] private IntRange _length = new(1, 3);
        [Tooltip("Chance a branch turns at each room. 0 = straight wings, 1 = twisting")]
        [SerializeField, Range(0f, 1f)] private float _winding = 0.3f;
        [Tooltip("Chance a branch starts from another branch's room instead of the main path, growing deeper side areas")]
        [SerializeField, Range(0f, 1f)] private float _forkChance = 0.25f;
        [Tooltip("Branches that start from the Start room before any others, making Start a hub")]
        [SerializeField] private IntRange _hubBranches = new(0, 0);
        [Tooltip("Chance a branch's last room also links to a neighbouring room, closing a loop instead of dead-ending")]
        [SerializeField, Range(0f, 1f)] private float _rejoinChance = 0.3f;

        public IntRange Length       => _length;
        public float    Winding      => _winding;
        public float    ForkChance   => _forkChance;
        public IntRange HubBranches  => _hubBranches;
        public float    RejoinChance => _rejoinChance;
    }
}
