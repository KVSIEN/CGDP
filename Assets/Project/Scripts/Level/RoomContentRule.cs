using System;
using UnityEngine;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // What a room of one MapNodeType is filled with when the level is built. Where it is
    // (the place) comes from the room's category, not its type.
    [Serializable]
    public class RoomContentRule
    {
        public MapNodeType Type;

        [Tooltip("Enemy prefabs to pick from (each needs EnemyAI)")]
        public GameObject[] Enemies = Array.Empty<GameObject>();
        [Tooltip("Enemy count, read at the room's intensity: Min in the calmest rooms, Max at peak")]
        public IntRange EnemyCount;

        [Tooltip("Placed in the middle of the room (a chest, a crafting station, a boss)")]
        public GameObject Centerpiece;
        [Tooltip("Cover, crates and clutter placed around the room by rules (against walls, in corners, breaking sightlines…). Shared by the room types that list it")]
        public RoomDressing Dressing;

        [Tooltip("One of these is always placed in every room of this type, picked at random (a Quiet room's resource, ammo cache or loot), in a nook against a wall with floor in front")]
        public GameObject[] Offerings = Array.Empty<GameObject>();
    }
}
