using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // One kind of furnishing a room function places: which prefabs, how many, and which
    // tiles suit it (by tag and zone).
    [Serializable]
    public class RoomPropPlacement
    {
        [Tooltip("Picked at random for each one placed. Origin at the base, +Z the front")]
        public GameObject[] Prefabs = Array.Empty<GameObject>();
        public IntRange Count = new(2, 4);
        [Tooltip("Only on tiles with one of these tags (Edge = along the walls). Nothing = anywhere")]
        public RoomTileTags On;
        [Tooltip("Never on tiles with any of these tags")]
        public RoomTileTags Avoid = RoomTileTags.Walkway | RoomTileTags.NearDoor | RoomTileTags.Centre | RoomTileTags.Structure | RoomTileTags.Prop;
        public PropZone Zone;
        public PropFacing Facing;
        [Tooltip("Free tiles kept between two of these (0 = may stand side by side)")]
        [Min(0)] public int Spacing = 1;
    }
}
