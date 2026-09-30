using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Builds floors, walls, room structure (pillars, inner walls) and doors for a
    // LevelLayout under a parent transform. Walls come from LevelWallBuilder; doorways are
    // simply where a corridor tile meets a room tile.
    public class LevelGeometryBuilder
    {
        private static readonly Vector2Int[] Neighbours =
        {
            new(-1, -1), new(0, -1), new(1, -1), new(-1, 0), new(1, 0), new(-1, 1), new(0, 1), new(1, 1),
        };

        private readonly LevelBuildSettings _settings;

        public LevelGeometryBuilder(LevelBuildSettings settings) => _settings = settings;

        public void Build(LevelLayout layout, Transform parent)
        {
            var roomFloors     = new BoxMeshBuilder();
            var corridorFloors = new BoxMeshBuilder();
            var walls          = new BoxMeshBuilder();
            var structure      = new BoxMeshBuilder();

            AddFloors(layout, roomFloors, corridorFloors);
            new LevelWallBuilder(_settings, layout, walls).Build();
            foreach (LevelRoom room in layout.Rooms.Values)
                if (room.Structure != null) AddStructure(layout, room, structure, parent);

            CreatePart("RoomFloors",     roomFloors,     _settings.FloorMaterial,         parent);
            CreatePart("CorridorFloors", corridorFloors, _settings.CorridorFloorMaterial, parent);
            CreatePart("Walls",          walls,          _settings.WallMaterial,          parent);
            CreatePart("Structure",      structure,      _settings.WallMaterial,          parent);
            PlaceDoors(layout, parent);
        }

        // Curved-wall rooms also get floor on the ring of tiles around them: their diagonal
        // walls cut through those tiles.
        private void AddFloors(LevelLayout layout, BoxMeshBuilder roomFloors, BoxMeshBuilder corridorFloors)
        {
            var extra = new HashSet<Vector2Int>();
            foreach (Vector2Int t in layout.WalkableTiles)
            {
                AddFloorTile(layout, layout.IsCorridor(t) ? corridorFloors : roomFloors, t);

                LevelRoom room = layout.RoomAt(t);
                if (room == null || !room.Footprint.CurvedWalls) continue;
                foreach (Vector2Int n in Neighbours)
                    if (!layout.IsWalkable(t + n)) extra.Add(t + n);
            }
            foreach (Vector2Int t in extra) AddFloorTile(layout, roomFloors, t);
        }

        private void AddFloorTile(LevelLayout layout, BoxMeshBuilder floors, Vector2Int t)
        {
            float tile = layout.TileSize, floor = _settings.FloorThickness;
            floors.AddBox(layout.TileToLocal(t) + Vector3.down * (floor * 0.5f), new Vector3(tile, floor, tile));
        }

        private void AddStructure(LevelLayout layout, LevelRoom room, BoxMeshBuilder boxes, Transform parent)
        {
            float tile = layout.TileSize;
            float height = room.WallHeight;

            foreach (StructurePillar pillar in room.Structure.Pillars)
            {
                Vector3 foot = new Vector3(pillar.Position.x, 0f, pillar.Position.y) * tile;
                if (pillar.Prefab != null)
                    Object.Instantiate(pillar.Prefab, parent.TransformPoint(foot), parent.rotation, parent);
                else
                    boxes.AddBox(foot + Vector3.up * (height * 0.5f), new Vector3(pillar.Width, height, pillar.Width));
            }

            // Inner walls stand centred on the edge between their two tiles.
            float thickness = _settings.WallThickness;
            foreach (PartitionEdge edge in room.Structure.Partitions)
            {
                float h = edge.Height > 0f ? Mathf.Min(edge.Height, height) : height;
                Vector3 centre = (layout.TileToLocal(edge.Tile) + layout.TileToLocal(edge.Other)) * 0.5f + Vector3.up * (h * 0.5f);
                Vector3 size = edge.Side.x != 0 ? new Vector3(thickness, h, tile + thickness) : new Vector3(tile + thickness, h, thickness);
                boxes.AddBox(centre, size);
            }
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
