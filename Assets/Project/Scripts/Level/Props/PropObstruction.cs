using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // Whether a planned prop's solid colliders, at their true turn, reach into anything built in
    // the room: its walls (straight, slanted or inner), a 45° wall, a pillar, a ledge guard, a
    // flight of stairs or the face of a floor on another level. Points are sampled across each
    // collider's box seen from above, so a prop turned 45° is read by its real outline, not the
    // square round it. Planning keeps props on free tiles of one level and off the walls beside
    // them (PropWallClearance); this checks the result against everything at once.
    internal static class PropObstruction
    {
        // How deep a prop may reach into a wall before it shows, in tiles: the clearance stands
        // props a little inside the thickness of an inner wall, and 6 cm is never seen.
        private const float Tolerance = 0.02f;
        // Inner and 45° walls: 0.3 m thick on a 3 m tile.
        private const float WallHalf = 0.05f;
        // The room's own slanted walls, measured from their floor side (PropWallClearance).
        private const float SlantHalf = PropWallClearance.WallHalfThickness;
        // A ledge's half wall stands 0.2 m into the higher tile (a railing a little less).
        private const float GuardDepth = PropWallClearance.GuardDepth;
        // Between sample points, in tiles (about 20 cm).
        private const float Spacing = 0.07f;

        // What the prop reaches into ("a ledge guard at (3, 4)"), or null when it stands clear.
        // A decoration has no colliders to collide with. `boxes` is scratch space.
        public static string Find(RoomStructure structure, PlannedProp prop, List<Vector2[]> boxes)
        {
            if (prop.Decor || !PropReach.TryGetFootprints(prop.Prefab, prop.Yaw, prop.Stretch, boxes)) return null;

            Vector2Int home = HomeOf(structure, prop);
            if (!structure.Contains(home)) return null;
            int level = structure.LevelOf(home);

            foreach (Vector2[] box in boxes)
            {
                Vector2 origin = prop.Position + box[0] / PropWallClearance.TileMetres;
                Vector2 across = (box[1] - box[0]) / PropWallClearance.TileMetres;
                Vector2 deep = (box[2] - box[0]) / PropWallClearance.TileMetres;
                int stepsAcross = Mathf.Max(1, Mathf.CeilToInt(across.magnitude / Spacing));
                int stepsDeep = Mathf.Max(1, Mathf.CeilToInt(deep.magnitude / Spacing));
                for (int i = 0; i <= stepsAcross; i++)
                    for (int j = 0; j <= stepsDeep; j++)
                    {
                        Vector2 point = origin + across * ((float)i / stepsAcross) + deep * ((float)j / stepsDeep);
                        string hit = HitAt(structure, point, level);
                        if (hit != null) return hit;
                    }
            }
            return null;
        }

        // The tile the prop stands on: under its middle, else the first of the tiles it claimed.
        private static Vector2Int HomeOf(RoomStructure structure, PlannedProp prop)
        {
            var under = new Vector2Int(Mathf.FloorToInt(prop.Position.x), Mathf.FloorToInt(prop.Position.y));
            return structure.Contains(under) || prop.Tiles.width == 0 ? under : prop.Tiles.min;
        }

        private static string HitAt(RoomStructure structure, Vector2 point, int level)
        {
            var tile = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            if (Depth(point, tile) > Tolerance)
            {
                if (!structure.Contains(tile)) return $"the room's wall at {tile}";
                if (structure.TryGetStairs(tile, out _)) return $"a flight of stairs at {tile}";
                if (structure.LevelOf(tile) != level) return $"the edge of a floor on another level at {tile}";
            }

            foreach (Vector2Int side in GridNeighbours.Four)
            {
                Vector2Int next = tile + side;
                if (!structure.Contains(next)) continue;
                float toEdge = ToEdge(point, tile, side);
                if (structure.IsPartitioned(tile, next) && toEdge < WallHalf - Tolerance) return $"an inner wall at {tile}";
                if (structure.LevelOf(tile) > structure.LevelOf(next) && structure.GuardOn(tile, next) != LedgeGuard.None
                    && toEdge < GuardDepth - Tolerance)
                    return $"a ledge guard at {tile}";
            }

            foreach (Vector2Int step in GridNeighbours.Eight)
                if (TryDiagonalNear(structure, tile + step, point, out Vector2Int at)) return $"a 45° wall at {at}";
            if (TryDiagonalNear(structure, tile, point, out Vector2Int own)) return $"a 45° wall at {own}";

            foreach (var (from, to) in structure.SlantedWalls)
            {
                Vector2 along = to - from;
                float length = along.magnitude;
                Vector2 dir = along / length, inward = new(-dir.y, dir.x);
                float position = Vector2.Dot(point - from, dir), side = Vector2.Dot(point - from, inward);
                // Beside the wall's run, on its floor side but inside its thickness, or just beyond it.
                if (position > 0f && position < length && side > -1f && side < SlantHalf - Tolerance) return $"a slanted wall near {tile}";
            }

            foreach (StructurePillar pillar in structure.Pillars)
            {
                float half = pillar.Width / PropWallClearance.TileMetres * 0.5f - Tolerance;
                Vector2 off = point - pillar.Position;
                if (Mathf.Abs(off.x) < half && Mathf.Abs(off.y) < half) return $"a pillar at {Vector2Int.FloorToInt(pillar.Position)}";
            }
            return null;
        }

        private static bool TryDiagonalNear(RoomStructure structure, Vector2Int tile, Vector2 point, out Vector2Int at)
        {
            at = tile;
            return structure.TryGetDiagonal(tile, out DiagonalWall wall) && ToSegment(point, wall.From, wall.To) < WallHalf - Tolerance;
        }

        // How far inside `tile` the point lies (0 on its border).
        private static float Depth(Vector2 point, Vector2Int tile) =>
            Mathf.Min(Mathf.Min(point.x - tile.x, tile.x + 1 - point.x), Mathf.Min(point.y - tile.y, tile.y + 1 - point.y));

        // Distance from the point to the tile's border on `side`.
        private static float ToEdge(Vector2 point, Vector2Int tile, Vector2Int side) =>
            side.x > 0 ? tile.x + 1 - point.x : side.x < 0 ? point.x - tile.x : side.y > 0 ? tile.y + 1 - point.y : point.y - tile.y;

        private static float ToSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 along = to - from;
            float t = Mathf.Clamp01(Vector2.Dot(point - from, along) / along.sqrMagnitude);
            return Vector2.Distance(point, from + along * t);
        }
    }
}
