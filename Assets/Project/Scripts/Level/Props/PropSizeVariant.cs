using System;
using UnityEngine;

namespace CGD.Level
{
    // Another size of a prop, and how often it stands in the prop's place (PropSizeVariants).
    [Serializable]
    public struct PropSizeVariant
    {
        public GameObject Prefab;
        [Min(0f)] public float Weight;
        [Tooltip("Only in rooms whose walls are at least this high, in metres: a tall tree in a tall room. 0 = any room")]
        [Min(0f)] public float MinRoomHeight;
        [Tooltip("In a group, how far in metres the pieces round this size move out from the listed size's places: x for those beside it, y for those in front or behind (negative = in). Usually half of how much wider and deeper it is")]
        public Vector2 MemberShift;
        [Tooltip("Floor tiles this size needs, across and front to back, where it is more than the placement gives: a long container. It only stands where that floor is free. 0 = the placement's footprint")]
        public Vector2Int Footprint;

        public PropSizeVariant(GameObject prefab, float weight)
        {
            Prefab = prefab;
            Weight = weight;
            MinRoomHeight = 0f;
            MemberShift = Vector2.zero;
            Footprint = Vector2Int.zero;
        }
    }
}
