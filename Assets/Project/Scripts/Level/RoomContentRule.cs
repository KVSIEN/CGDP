using System;
using UnityEngine;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // What a room of one MapNodeType looks like and is filled with when the level is built.
    [Serializable]
    public class RoomContentRule
    {
        public MapNodeType Type;

        [Tooltip("Floor plans for this room type. Empty = the build settings' shapes")]
        public RoomShape[] Shapes = Array.Empty<RoomShape>();

        [Tooltip("Enemy prefabs to pick from (each needs EnemyAI)")]
        public GameObject[] Enemies = Array.Empty<GameObject>();
        [Tooltip("Enemy count, read at the room's intensity: Min in the calmest rooms, Max at peak")]
        public IntRange EnemyCount;

        [Tooltip("Placed in the middle of the room (a chest, a crafting station, a boss)")]
        public GameObject Centerpiece;
        [Tooltip("Scattered around the room (crates, cover, breakables)")]
        public GameObject[] Props = Array.Empty<GameObject>();
        public IntRange PropCount;
    }
}
