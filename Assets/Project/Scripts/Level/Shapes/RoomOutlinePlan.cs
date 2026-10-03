using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A room's wall line (RoomOutline), plus what its chamfers cut away: the triangles
    // between each diagonal and the old corner, and the tiles they reach into — floor that
    // is now partly behind a wall, so nothing should be placed there — and the tiles on
    // either side of each cut, the room's new corners. Tile units.
    public class RoomOutlinePlan
    {
        public List<(Vector2 from, Vector2 to)> Segments { get; } = new();
        public List<(Vector2 a, Vector2 corner, Vector2 b)> Cuts { get; } = new();
        public HashSet<Vector2Int> CutTiles { get; } = new();
        public HashSet<Vector2Int> CornerTiles { get; } = new();
    }
}
