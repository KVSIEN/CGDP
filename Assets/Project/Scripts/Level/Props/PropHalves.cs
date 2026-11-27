using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // The open halves of a room's diagonal walls' tiles (DiagonalWall) a small prop can stand in
    // (RoomPropPlacement.HalfTile): the nook behind an angled corner, the inside of a chevron's V,
    // the triangles beside an angled counter. A half is open where at least one of the two tiles
    // it opens onto is the room's floor at the wall's level, not solid, and in one of `zones`
    // (null = any); a half facing only the room's wall or other structure is shut in.
    internal static class PropHalves
    {
        public static List<(Vector2Int tile, Vector2Int normal)> Open(RoomStructure structure, ICollection<int> zones)
        {
            var halves = new List<(Vector2Int, Vector2Int)>();
            foreach (DiagonalWall wall in structure.Diagonals)
            {
                Vector2Int normal = wall.Rising ? new Vector2Int(1, -1) : new Vector2Int(1, 1);
                foreach (Vector2Int facing in new[] { normal, -normal })
                    if (!structure.HasHalfProp(wall.Tile, facing) && OpensOntoFloor(structure, wall, facing, zones))
                        halves.Add((wall.Tile, facing));
            }
            return halves;
        }

        // Facing straight out of the wall, away from it (yaw 0 = +y, 90 = +x).
        public static float YawOf(Vector2Int normal) => Mathf.Atan2(normal.x, normal.y) * Mathf.Rad2Deg;

        private static bool OpensOntoFloor(RoomStructure structure, DiagonalWall wall, Vector2Int normal, ICollection<int> zones)
        {
            var (a, b) = DiagonalWall.SidesOf(normal);
            foreach (Vector2Int side in new[] { a, b })
            {
                Vector2Int tile = wall.Tile + side;
                if (!structure.Contains(tile) || structure.Has(tile, RoomTileTags.Structure)) continue;
                if (structure.LevelOf(tile) != structure.LevelOf(wall.Tile) || structure.IsPartitioned(wall.Tile, tile)) continue;
                if (zones != null && !zones.Contains(structure.ZoneOf(tile))) continue;
                return true;
            }
            return false;
        }
    }
}
