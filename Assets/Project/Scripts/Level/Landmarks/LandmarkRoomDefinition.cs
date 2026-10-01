using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A hand-built room — reactor core, bridge, boss arena — placed instead of a generated
    // one. The generator still builds its rectangular floor and walls (so doorways line up
    // with corridors) and places the prefab inside as its interior. Doorways only go where
    // listed; tiles the prefab fills are kept free of spawns.
    [CreateAssetMenu(fileName = "LandmarkRoom", menuName = "CGD/Level/Landmark Room")]
    public class LandmarkRoomDefinition : ScriptableObject
    {
        [Tooltip("The interior. Origin at the room's south-west floor corner, +X east, +Z north")]
        [SerializeField] private GameObject _prefab;
        [Tooltip("Floor size in tiles. Must fit the room function's cells")]
        [SerializeField] private Vector2Int _sizeTiles = new(12, 12);
        [SerializeField] private List<LandmarkDoorway> _doorways = new();
        [Tooltip("Tiles the prefab's furniture stands on — no enemies, props or pillars go there")]
        [SerializeField] private List<Vector2Int> _occupiedTiles = new();

        public GameObject Prefab => _prefab;
        public Vector2Int SizeTiles => _sizeTiles;
        public IReadOnlyList<LandmarkDoorway> Doorways => _doorways;
        public IReadOnlyList<Vector2Int> OccupiedTiles => _occupiedTiles;
    }
}
