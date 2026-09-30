using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // All the level's walls as boxes: one on every edge between walkable and non-walkable
    // ground, as high as the room it bounds (corridors use the default height), merged
    // into one box per unbroken run. Rooms with curved walls follow RoomOutline instead,
    // and doorways into rooms taller than the corridor get a lintel to close the gap above.
    public class LevelWallBuilder
    {
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly LevelBuildSettings _settings;
        private readonly LevelLayout _layout;
        private readonly BoxMeshBuilder _walls;

        public LevelWallBuilder(LevelBuildSettings settings, LevelLayout layout, BoxMeshBuilder walls)
        {
            _settings = settings;
            _layout   = layout;
            _walls    = walls;
        }

        private float Tile      => _layout.TileSize;
        private float Thickness => _settings.WallThickness;

        public void Build()
        {
            AddTileEdgeWalls();
            foreach (LevelRoom room in _layout.Rooms.Values)
                if (room.Footprint.CurvedWalls) AddCurvedWalls(room);
            AddLintels();
        }

        // Collects the wall edges per side, line and height, then merges each unbroken run.
        private void AddTileEdgeWalls()
        {
            var edges = new Dictionary<(int side, int line, float height), List<int>>();
            foreach (Vector2Int t in _layout.WalkableTiles)
            {
                LevelRoom room = _layout.RoomAt(t);
                if (room != null && room.Footprint.CurvedWalls) continue;
                float height = room != null ? room.WallHeight : _settings.WallHeight;

                for (int side = 0; side < Directions.Length; side++)
                {
                    Vector2Int dir = Directions[side];
                    if (_layout.IsWalkable(t + dir)) continue;

                    bool alongZ = dir.x != 0;
                    var key = (side, alongZ ? t.x : t.y, height);
                    if (!edges.TryGetValue(key, out List<int> positions)) edges[key] = positions = new List<int>();
                    positions.Add(alongZ ? t.y : t.x);
                }
            }

            foreach (var ((side, line, height), positions) in edges)
            {
                positions.Sort();
                int runStart = positions[0];
                for (int i = 1; i <= positions.Count; i++)
                {
                    if (i < positions.Count && positions[i] == positions[i - 1] + 1) continue;
                    AddEdgeRun(Directions[side], line, runStart, positions[i - 1], height);
                    if (i < positions.Count) runStart = positions[i];
                }
            }
        }

        // Along the outer edge of tiles first..last on one line, overlapping the neighbouring
        // walls at the ends so corners leave no gap.
        private void AddEdgeRun(Vector2Int dir, int line, int first, int last, float height)
        {
            bool alongZ = dir.x != 0;
            Vector2Int a = alongZ ? new Vector2Int(line, first) : new Vector2Int(first, line);
            Vector2Int b = alongZ ? new Vector2Int(line, last)  : new Vector2Int(last, line);
            Vector3 centre  = (_layout.TileToLocal(a) + _layout.TileToLocal(b)) * 0.5f;
            Vector3 outward = new(dir.x, 0f, dir.y);
            float length = (last - first + 1) * Tile + Thickness * 2f;

            Vector3 position = centre + outward * ((Tile + Thickness) * 0.5f) + Vector3.up * (height * 0.5f);
            Vector3 size = alongZ ? new Vector3(Thickness, height, length) : new Vector3(length, height, Thickness);
            _walls.AddBox(position, size);
        }

        // Straight pieces and diagonals from the room's smoothed outline. The ground just
        // outside gets floor too, so no gap shows where a diagonal leaves the tile grid.
        private void AddCurvedWalls(LevelRoom room)
        {
            foreach ((Vector2 from, Vector2 to) in RoomOutline.Build(room.Footprint, (tile, side) => _layout.IsWalkable(tile + side)))
                AddSegment(from, to, room.WallHeight);
        }

        // The floor is on the segment's left; the wall sits just outside the line.
        private void AddSegment(Vector2 from, Vector2 to, float height)
        {
            Vector3 a = new Vector3(from.x, 0f, from.y) * Tile;
            Vector3 b = new Vector3(to.x, 0f, to.y) * Tile;
            Vector3 along = b - a;
            float length = along.magnitude;
            if (length < 0.001f) return;

            Vector3 dir = along / length;
            Vector3 outward = new(dir.z, 0f, -dir.x);
            Vector3 position = (a + b) * 0.5f + outward * (Thickness * 0.5f) + Vector3.up * (height * 0.5f);
            _walls.AddBox(position, new Vector3(Thickness, height, length + Thickness), Quaternion.LookRotation(dir));
        }

        // Room walls stand taller than the corridor's; above each doorway the wall carries on.
        private void AddLintels()
        {
            float corridorHeight = _settings.WallHeight;
            foreach (LevelDoorway doorway in _layout.Doorways)
            {
                float extra = doorway.Room.WallHeight - corridorHeight;
                if (extra <= 0.01f) continue;

                Vector2Int dir = doorway.Outward;
                Vector3 outward = new(dir.x, 0f, dir.y);
                Vector3 position = _layout.TileToLocal(doorway.RoomTile) + outward * ((Tile + Thickness) * 0.5f)
                                 + Vector3.up * (corridorHeight + extra * 0.5f);
                Vector3 size = dir.x != 0 ? new Vector3(Thickness, extra, Tile) : new Vector3(Tile, extra, Thickness);
                _walls.AddBox(position, size);
            }
        }
    }
}
