using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using CGD.Factions;
using CGD.Interaction;

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
            var factionFloors  = new Dictionary<FactionDefinition, BoxMeshBuilder>();
            var corridorFloors = new BoxMeshBuilder();
            var walls          = new BoxMeshBuilder();
            var structure      = new BoxMeshBuilder();

            Dictionary<Vector2Int, LevelRoom> outerRing = AddFloors(layout, roomFloors, factionFloors, corridorFloors);
            new LevelWallBuilder(_settings, layout, walls).Build();
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                if (room.Structure != null) AddStructure(layout, room, structure, parent);
                if (room.Landmark != null) PlaceLandmark(layout, room, parent);
            }

            CreatePart("RoomFloors",     roomFloors,     _settings.FloorMaterial,         parent);
            foreach (var (faction, floors) in factionFloors)
                CreatePart($"RoomFloors_{faction.name}", floors, TintedFloor(faction), parent);
            CreatePart("CorridorFloors", corridorFloors, _settings.CorridorFloorMaterial, parent);
            CreatePart("Walls",          walls,          _settings.WallMaterial,          parent);
            CreatePart("Structure",      structure,      _settings.WallMaterial,          parent);
            if (_settings.BuildCeilings) BuildCeilings(layout, outerRing, parent);
            PlaceDoors(layout, parent);
        }

        // Curved-wall rooms also get floor on the ring of tiles around them: their diagonal
        // walls cut through those tiles. Returns that ring, with the room each tile is for.
        // Faction-held rooms get their own floor mesh in the faction's tint.
        private Dictionary<Vector2Int, LevelRoom> AddFloors(LevelLayout layout, BoxMeshBuilder roomFloors,
            Dictionary<FactionDefinition, BoxMeshBuilder> factionFloors, BoxMeshBuilder corridorFloors)
        {
            BoxMeshBuilder FloorsOf(LevelRoom room)
            {
                if (room?.Faction == null || room.Faction.FloorTint <= 0f) return roomFloors;
                if (!factionFloors.TryGetValue(room.Faction, out BoxMeshBuilder floors))
                    factionFloors[room.Faction] = floors = new BoxMeshBuilder();
                return floors;
            }

            var extra = new Dictionary<Vector2Int, LevelRoom>();
            foreach (Vector2Int t in layout.WalkableTiles)
            {
                LevelRoom room = layout.RoomAt(t);
                AddFloorTile(layout, layout.IsCorridor(t) ? corridorFloors : FloorsOf(room), t);

                if (room == null || !room.Footprint.CurvedWalls) continue;
                foreach (Vector2Int n in Neighbours)
                    if (!layout.IsWalkable(t + n)) extra[t + n] = room;
            }
            foreach (var (t, room) in extra) AddFloorTile(layout, FloorsOf(room), t);
            return extra;
        }

        // A copy of the floor material shifted toward the faction's colour.
        private Material TintedFloor(FactionDefinition faction)
        {
            if (_settings.FloorMaterial == null) return null;
            var material = new Material(_settings.FloorMaterial) { name = $"{_settings.FloorMaterial.name} ({faction.DisplayName})" };
            material.color = Color.Lerp(material.color, faction.Color, faction.FloorTint);
            return material;
        }

        // A slab on top of every tile at its room's wall height (corridors at the default),
        // merged into one box per row of equal height. Rooms with an open ceiling stay open.
        // The ceiling is left out of the NavMesh so nothing walks on the roof.
        private void BuildCeilings(LevelLayout layout, Dictionary<Vector2Int, LevelRoom> outerRing, Transform parent)
        {
            var rows = new Dictionary<(int y, float height), List<int>>();
            void Add(Vector2Int t, LevelRoom room)
            {
                if (room != null && room.Function != null && room.Function.OpenCeiling) return;
                var key = (t.y, room != null ? room.WallHeight : _settings.WallHeight);
                if (!rows.TryGetValue(key, out List<int> xs)) rows[key] = xs = new List<int>();
                xs.Add(t.x);
            }
            foreach (Vector2Int t in layout.WalkableTiles) Add(t, layout.RoomAt(t));
            foreach (var (t, room) in outerRing) Add(t, room);

            var ceilings = new BoxMeshBuilder();
            float tile = layout.TileSize, thickness = _settings.FloorThickness;
            foreach (var ((y, height), xs) in rows)
            {
                xs.Sort();
                int start = xs[0];
                for (int i = 1; i <= xs.Count; i++)
                {
                    if (i < xs.Count && xs[i] == xs[i - 1] + 1) continue;
                    Vector3 centre = (layout.TileToLocal(new Vector2Int(start, y)) + layout.TileToLocal(new Vector2Int(xs[i - 1], y))) * 0.5f;
                    ceilings.AddBox(centre + Vector3.up * (height + thickness * 0.5f), new Vector3((xs[i - 1] - start + 1) * tile, thickness, tile));
                    if (i < xs.Count) start = xs[i];
                }
            }

            GameObject part = CreatePart("Ceilings", ceilings, _settings.CeilingMaterial, parent, _settings.CeilingLayer);
            if (part != null) part.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        }

        private void AddFloorTile(LevelLayout layout, BoxMeshBuilder floors, Vector2Int t)
        {
            float tile = layout.TileSize, floor = _settings.FloorThickness;
            floors.AddBox(layout.TileToLocal(t) + Vector3.down * (floor * 0.5f), new Vector3(tile, floor, tile));
        }

        // The interior prefab's origin goes on the room's south-west floor corner.
        private static void PlaceLandmark(LevelLayout layout, LevelRoom room, Transform parent)
        {
            if (room.Landmark.Prefab == null) return;
            Vector2Int min = room.Footprint.Bounds.min;
            Vector3 corner = new Vector3(min.x, 0f, min.y) * layout.TileSize;
            Object.Instantiate(room.Landmark.Prefab, parent.TransformPoint(corner), parent.rotation, parent);
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
                GameObject door = Object.Instantiate(prefab, parent.TransformPoint(position), parent.rotation * Quaternion.LookRotation(outward), parent);

                // The door sits at the end nearer Start; a one-way door opens from the corridor
                // side, i.e. for someone arriving from the deeper room.
                if (doorway.Connection.OneWay) MakeOneWay(layout, doorway, door, parent.TransformDirection(outward));
            }
        }

        private static void MakeOneWay(LevelLayout layout, LevelDoorway doorway, GameObject door, Vector3 openSide)
        {
            Door hinge = door.GetComponentInChildren<Door>();
            if (hinge != null)
                hinge.MakeOneWay(openSide);
            else
                layout.Warnings.Add($"One-way #{doorway.Connection.A}–#{doorway.Connection.B} is open both ways: its door prefab has no Door.");
        }

        private GameObject CreatePart(string name, BoxMeshBuilder builder, Material material, Transform parent, int layer = -1)
        {
            if (builder.IsEmpty) return null;

            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.layer = layer >= 0 ? layer : _settings.GeometryLayer;
            go.isStatic = true;
            go.transform.SetParent(parent, false);

            Mesh mesh = builder.ToMesh(name);
            go.GetComponent<MeshFilter>().sharedMesh     = mesh;
            go.GetComponent<MeshCollider>().sharedMesh   = mesh;
            if (material != null) go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
