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

        [Tooltip("What rooms of this type can be (Lobby, Park…), picked by weight. Empty = plain rooms from the build settings' shapes")]
        public RoomFunction[] Functions = Array.Empty<RoomFunction>();

        [Tooltip("Enemy prefabs to pick from (each needs EnemyAI)")]
        public GameObject[] Enemies = Array.Empty<GameObject>();
        [Tooltip("Enemy count, read at the room's intensity: Min in the calmest rooms, Max at peak")]
        public IntRange EnemyCount;
        [Tooltip("Chance (0–1) each enemy is tier 2, read at the room's intensity like the count. Faction rooms only")]
        public FloatRange Tier2Chance;
        [Tooltip("Chance (0–1) each enemy is tier 3, read at the room's intensity")]
        public FloatRange Tier3Chance;

        [Tooltip("Placed in the middle of the room (a chest, a crafting station, a boss)")]
        public GameObject Centerpiece;
        [Tooltip("Scattered around the room (crates, cover, breakables)")]
        public GameObject[] Props = Array.Empty<GameObject>();
        public IntRange PropCount;

        [Tooltip("One of these is always placed in every room of this type, picked at random (a Quiet room's resource, ammo cache or loot)")]
        public GameObject[] Offerings = Array.Empty<GameObject>();
    }
}
