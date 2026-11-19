using System;
using UnityEngine;

namespace CGD.Level
{
    // One kind of small walled structure a room can hold (a restroom block, a bar hut, a
    // kiosk): its size, how likely it is, and the place that furnishes its inside.
    [Serializable]
    public class PavilionKind
    {
        [Tooltip("Furnishes the inside, the way a building's rooms are furnished by their places")]
        public RoomFunction Function;
        [Tooltip("Shown in the Room Lab's spaces")]
        public string Name = "Pavilion";
        [Tooltip("Smallest inside, in tiles (width × depth); turned to fit")]
        public Vector2Int MinSize = new(2, 2);
        [Tooltip("Largest inside, in tiles")]
        public Vector2Int MaxSize = new(3, 3);
        [Min(0f)] public float Weight = 1f;
    }
}
