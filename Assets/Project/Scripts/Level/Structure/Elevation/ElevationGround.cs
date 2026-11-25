using UnityEngine;

namespace CGD.Level
{
    // What every elevation feature is built on.
    internal static class ElevationGround
    {
        // Free floor nothing keeps clear, still on the doorways' level.
        public static bool IsBare(RoomStructure structure, Vector2Int tile) =>
            structure.IsOpen(tile) && structure.IsFree(tile) && structure.LevelOf(tile) == 0;
    }
}
