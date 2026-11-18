using System;
using UnityEngine;

namespace CGD.Level
{
    // One piece of a prop group, set around the placement's own piece on the same tiles: a
    // chair at a card table, a stool along a bar, a lamp beside a reading chair. It turns with
    // the group, so the group reads the same whichever way it faces.
    [Serializable]
    public class PropMember
    {
        [Tooltip("Picked at random for each group")]
        public GameObject[] Prefabs = Array.Empty<GameObject>();
        [Tooltip("Tiles from the middle of the group's footprint: x to its right, y to its front (+0.35 = a chair a metre in front of a 1×1 table)")]
        public Vector2 Offset;
        [Tooltip("Degrees, turned from the group's facing (180 = facing back toward the group's middle from in front of it)")]
        public float Yaw;
        [Tooltip("How likely this piece is in each group, so groups vary (a table with two to four chairs). 1 = always")]
        [Range(0f, 1f)] public float Chance = 1f;
    }
}
