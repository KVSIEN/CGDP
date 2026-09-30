using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // What a room is on the ship — a lobby, a restaurant, a park — and so how it's built:
    // which floor plans suit it, how big and tall it is and what structure it has. A room
    // type (Combat, Shop…) lists the functions it can be.
    [CreateAssetMenu(fileName = "RoomFunction", menuName = "CGD/Level/Room Function")]
    public class RoomFunction : ScriptableObject
    {
        [SerializeField] private string _displayName = "Room";
        [Tooltip("How often this function is picked relative to the others a room type lists")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Floor plans for this kind of room. Empty = the build settings' shapes")]
        [SerializeField] private RoomShape[] _shapes = Array.Empty<RoomShape>();
        [Tooltip("Width and depth in tiles, at most the build settings' Room Tiles. 0 = the full size")]
        [SerializeField] private IntRange _size;
        [Tooltip("Metres. 0 = the build settings' wall height")]
        [SerializeField, Min(0f)] private float _wallHeight;
        [Tooltip("Applied in order once doorways are known: pillars, dividers…")]
        [SerializeField] private RoomStructureRule[] _structure = Array.Empty<RoomStructureRule>();

        public string DisplayName => _displayName;
        public float  Weight      => _weight;
        public float  WallHeight  => _wallHeight;
        public IReadOnlyList<RoomShape>         Shapes    => _shapes;
        public IReadOnlyList<RoomStructureRule> Structure => _structure;

        // Tiles per side for one room, between minTiles and maxTiles.
        public int RollSize(RandomStream random, int minTiles, int maxTiles)
        {
            int size = _size.Max > 0 ? _size.Evaluate(random) : maxTiles;
            return Mathf.Clamp(size, minTiles, maxTiles);
        }
    }
}
