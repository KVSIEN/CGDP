using System;
using UnityEngine;

namespace CGD.Level
{
    // One lane of a terraced room (TerracesStructureRule): how high it stands and how wide it is
    // against the room's other lanes.
    [Serializable]
    public struct TerraceLane
    {
        [Tooltip("Floor level of the lane (-1, 0 or 1), before the whole room is shifted so its doorways open onto level 0")]
        [Range(RoomElevation.Lowest, RoomElevation.Highest)] public int Level;
        [Tooltip("Share of the room's width, against the other lanes' (at least two tiles wide whatever its share)")]
        [Min(0.1f)] public float Weight;

        public TerraceLane(int level, float weight)
        {
            Level  = level;
            Weight = weight;
        }
    }
}
