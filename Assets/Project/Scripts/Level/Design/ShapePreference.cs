using System;
using UnityEngine;

namespace CGD.Level
{
    // How much a faction favours one floor plan: its weight is multiplied by this wherever the
    // shape is a candidate for one of the faction's rooms.
    [Serializable]
    public class ShapePreference
    {
        public RoomShape Shape;
        [Tooltip("1 = unchanged, above 1 = more often, 0 = never")]
        [Min(0f)] public float Weight = 1f;
    }
}
