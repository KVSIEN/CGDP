using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Builds floors, walls and doors for a LevelLayout under a parent transform. A wall
    // goes on every edge between a walkable tile and a non-walkable one, so doorways are
    // simply the edges where a corridor tile meets a room tile, and any room shape gets
    // its outline walled.
    public class LevelGeometryBuilder
    {
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly LevelBuildSettings _settings;

        public LevelGeometryBuilder(LevelBuildSettings settings) => _settings = settings;

        public void Build(LevelLayout layout, Transform parent)
        {
            var roomFloors     = new BoxMeshBuilder();
            var corridorFloors = new BoxMeshBuilder();
            var walls          = new BoxMeshBuilder();

            float tile  = layout.TileSize;
            float floor = _settings.FloorThickness;

            foreach (Vector2Int t in layout.WalkableTiles)
            {
                var floors = layout.IsCorridor(t) ? corridorFloors : roomFloors;
                floors.AddBox(layout.TileToLocal(t) + Vector3.down * (floor * 0.5f), new Vector3(tile, floor, tile));
            }
            AddWalls(layout, walls);

            CreatePart("RoomFloors",     roomFloors,     _settings.FloorMaterial,         parent);
            CreatePart("CorridorFloors", corridorFloors, _settings.CorridorFloorMaterial, parent);
            CreatePart("Walls",          walls,          _settings.WallMaterial,          parent);
            PlaceDoors(layout, parent);
        }

        // Collects the wall edges per side and line, then merges each unbroken run into one
        // box — a long wall is one box, not one per tile, which keeps the mesh and its
        // collider small whatever shape the rooms have.
        private void AddWalls(LevelLayout layout, BoxMeshBuilder walls)
        {
            var edges = new Dictionary<(int side, int line), List<int>>();
            foreach (Vector2Int t in layout.WalkableTiles)
                for (int side = 0; side < Directions.Length; side++)
                {
                    Vector2Int dir = Directions[side];
                    if (layout.IsWalkable(t + dir)) continue;

                    bool alongZ = dir.x != 0;
                    var key = (side, alongZ ? t.x : t.y);
                    if (!edges.TryGetValue(key, out List<int> positions)) edges[key] = positions = new List<int>();
                    positions.Add(alongZ ? t.y : t.x);
                }

            foreach (var ((side, line), positions) in edges)
            {
                positions.Sort();
                int runStart = positions[0];
                for (int i = 1; i <= positions.Count; i++)
                {
                    if (i < positions.Count && positions[i] == positions[i - 1] + 1) continue;
                    AddWallRun(layout, walls, Directions[side], line, runStart, positions[i - 1]);
                    if (i < positions.Count) runStart = positions[i];
                }
            }
        }

        // A wall along the outer edge of tiles first..last on one line. It sits just outside
        // the tiles and overlaps its neighbours at the ends, so walls meeting at a corner
        // leave no gap.
        private void AddWallRun(LevelLayout layout, BoxMeshBuilder walls, Vector2Int dir, int line, int first, int last)
        {
            float tile      = layout.TileSize;
            float height    = _settings.WallHeight;
            float thickness = _settings.WallThickness;
            bool alongZ = dir.x != 0;

            Vector2Int a = alongZ ? new Vector2Int(line, first) : new Vector2Int(first, line);
            Vector2Int b = alongZ ? new Vector2Int(line, last)  : new Vector2Int(last, line);
            Vector3 centre  = (layout.TileToLocal(a) + layout.TileToLocal(b)) * 0.5f;
            Vector3 outward = new(dir.x, 0f, dir.y);
            float length = (last - first + 1) * tile + thickness * 2f;

            Vector3 position = centre + outward * ((tile + thickness) * 0.5f) + Vector3.up * (height * 0.5f);
            Vector3 size = alongZ ? new Vector3(thickness, height, length) : new Vector3(length, height, thickness);
            walls.AddBox(position, size);
        }

        private void PlaceDoors(LevelLayout layout, Transform parent)
        {
            foreach (LevelDoorway doorway in layout.Doorways)
            {
                if (!doorway.HasGate) continue;

                GameObject prefab = _settings.DoorPrefabFor(doorway.Connection.Type);
                if (prefab == null) continue;

                // On the edge between the room tile and the corridor tile, facing out of the room.
                Vector3 position = (layout.TileToLocal(doorway.RoomTile) + layout.TileToLocal(doorway.OutsideTile)) * 0.5f;
                Vector3 outward  = new(doorway.Outward.x, 0f, doorway.Outward.y);
                Object.Instantiate(prefab, parent.TransformPoint(position), parent.rotation * Quaternion.LookRotation(outward), parent);
            }
        }

        private void CreatePart(string name, BoxMeshBuilder builder, Material material, Transform parent)
        {
            if (builder.IsEmpty) return;

            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.layer = _settings.GeometryLayer;
            go.isStatic = true;
            go.transform.SetParent(parent, false);

            Mesh mesh = builder.ToMesh(name);
            go.GetComponent<MeshFilter>().sharedMesh     = mesh;
            go.GetComponent<MeshCollider>().sharedMesh   = mesh;
            if (material != null) go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
