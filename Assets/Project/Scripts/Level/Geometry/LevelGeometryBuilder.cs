using System.Collections.Generic;
using UnityEngine;
using CGD.Map;
using Unity.AI.Navigation;
using CGD.Enemies;
using CGD.Factions;
using CGD.Interaction;

namespace CGD.Level
{
    // Builds floors, walls, room structure (pillars, inner walls), the overgrowth on overgrown
    // rooms' floors and doors for a LevelLayout under a parent transform. Walls come from
    // LevelWallBuilder (dressed with the settings' wall kit when there is one: the boxes then
    // only collide); doorways are simply where a corridor tile meets a room tile.
    public class LevelGeometryBuilder
    {
        private static readonly Vector2Int[] Neighbours =
        {
            new(-1, -1), new(0, -1), new(1, -1), new(-1, 0), new(1, 0), new(-1, 1), new(0, 1), new(1, 1),
        };

        private readonly LevelBuildSettings _settings;
        private readonly GeneratedAssets _assets;

        // `assets` collects the meshes and materials made here, for the level's owner to free.
        public LevelGeometryBuilder(LevelBuildSettings settings, GeneratedAssets assets)
        {
            _settings = settings;
            _assets   = assets;
        }

        // The gate placed on each Locked or Secret connection, so the populator can wire
        // up what opens it (a terminal lock's terminals).
        public Dictionary<MapConnection, GameObject> Gates { get; } = new();

        // Connections whose door is replaced by ObjectiveDoorPrefab (the Boss room's, while it
        // waits on main objectives); the doors placed there end up in ObjectiveDoors.
        public HashSet<MapConnection> ObjectiveGated { get; } = new();
        public GameObject ObjectiveDoorPrefab { get; set; }
        public List<GameObject> ObjectiveDoors { get; } = new();

        public void Build(LevelLayout layout, Transform parent)
        {
            var roomFloors     = new BoxMeshBuilder();
            var factionFloors  = new Dictionary<FactionDefinition, BoxMeshBuilder>();
            var corridorFloors = new BoxMeshBuilder();
            var walls          = new BoxMeshBuilder();
            var structure      = new BoxMeshBuilder();
            var lowCover       = new BoxMeshBuilder();
            var stairs         = new BoxMeshBuilder();
            var railings       = new BoxMeshBuilder();
            var ground         = new BoxMeshBuilder();
            var decks          = new BoxMeshBuilder();
            var elevation      = new ElevationGeometryBuilder(_settings, layout);

            Dictionary<Vector2Int, LevelRoom> outerRing = AddFloors(layout, roomFloors, factionFloors, corridorFloors);
            LevelWallPlan wallPlan = LevelWallPlan.Create(layout, _settings);
            new LevelWallBuilder(_settings, layout, walls).Build(wallPlan);
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                if (room.Structure != null) AddStructure(layout, room, structure, lowCover, parent);
                elevation.Build(room, stairs, ground, railings, lowCover, decks, parent);
                if (room.Landmark != null) PlaceLandmark(layout, room, parent);
            }

            CreatePart("RoomFloors",     roomFloors,     _settings.FloorMaterial,         parent);
            foreach (var (faction, floors) in factionFloors)
                CreatePart($"RoomFloors_{faction.name}", floors, TintedFloor(faction), parent);
            CreatePart("CorridorFloors", corridorFloors, _settings.CorridorFloorMaterial, parent);
            GameObject wallBoxes = CreatePart("Walls", walls, _settings.WallMaterial, parent);
            if (wallBoxes != null) wallBoxes.AddComponent<BoundaryWall>();
            if (_settings.WallKit != null)
            {
                if (wallBoxes != null) wallBoxes.GetComponent<MeshRenderer>().enabled = false;
                PlaceWallKit(layout, wallPlan, parent);
            }
            CreatePart("Structure",      structure,      _settings.WallMaterial,          parent);
            CreatePart("LowCover",       lowCover,       _settings.LowCoverMaterial,      parent);
            CreatePart("Stairs",         stairs,         _settings.FloorMaterial,         parent);
            CreatePart("Slopes",         ground,         _settings.FloorMaterial,         parent);
            CreatePart("Railings",       railings,       _settings.LowCoverMaterial,      parent);
            CreatePart("Decks",          decks,          _settings.FloorMaterial,         parent);
            BuildOvergrowth(layout, parent);
            bool roofedBuildings = HasRoofedBuildings(layout);
            if (_settings.BuildCeilings || roofedBuildings) BuildCeilings(layout, outerRing, parent);
            if (roofedBuildings && _settings.LightBuildings) new BuildingLightBuilder(_settings).Build(layout, parent);
            if (_settings.BuildSkyDomes && _settings.SkyDomeMaterial != null) new SkyDomeBuilder(_settings, _assets).Build(layout, parent);
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
                AddFloorTile(layout, layout.IsCorridor(t) ? corridorFloors : FloorsOf(room), t, layout.IsCorridor(t) ? null : room?.Structure);

                if (room == null || !room.Footprint.CurvedWalls) continue;
                foreach (Vector2Int n in Neighbours)
                    if (!layout.IsWalkable(t + n)) extra[t + n] = room;
            }
            foreach (var (t, room) in extra) AddFloorTile(layout, FloorsOf(room), t, room.Structure);
            return extra;
        }

        // A copy of the floor material shifted toward the faction's colour.
        private Material TintedFloor(FactionDefinition faction)
        {
            if (_settings.FloorMaterial == null) return null;
            var material = _assets.Track(new Material(_settings.FloorMaterial) { name = $"{_settings.FloorMaterial.name} ({faction.DisplayName})" });
            material.color = Color.Lerp(material.color, faction.Color, faction.FloorTint);
            return material;
        }

        private static bool HasRoofedBuildings(LevelLayout layout)
        {
            foreach (LevelRoom room in layout.Rooms.Values)
                if (room.Structure != null && room.Structure.HasRoofedSpaces) return true;
            return false;
        }

        // A slab on top of every tile at its room's wall height (corridors at the default),
        // merged into one box per row of equal height. Rooms with an open ceiling stay open,
        // except for the buildings standing in them (a town's), which are roofed even when the
        // level builds no ceilings. The ceiling is left out of the NavMesh so nothing walks on
        // the roof.
        private void BuildCeilings(LevelLayout layout, Dictionary<Vector2Int, LevelRoom> outerRing, Transform parent)
        {
            var rows = new Dictionary<(int y, float height), List<int>>();
            void Add(Vector2Int t, LevelRoom room)
            {
                bool roofed = room != null && room.Structure != null && room.Structure.IsRoofed(t);
                if (!roofed && (!_settings.BuildCeilings || (room != null && room.Function != null && room.Function.OpenCeiling))) return;
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

        // The growth lying on overgrown rooms' floors (LevelRoom.Overgrowth): one mesh per look,
        // walked on and in the NavMesh like the floor under it.
        private void BuildOvergrowth(LevelLayout layout, Transform parent)
        {
            var looks = new List<(Material material, OvergrowthMeshBuilder mesh)>();
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                if (room.Overgrowth == null) continue;

                Material material = room.Overgrowth.Profile != null && room.Overgrowth.Profile.Material != null
                    ? room.Overgrowth.Profile.Material
                    : _settings.FloorMaterial;
                int look = looks.FindIndex(entry => entry.material == material);
                if (look < 0)
                {
                    looks.Add((material, new OvergrowthMeshBuilder()));
                    look = looks.Count - 1;
                }
                looks[look].mesh.AddRoom(layout, room);
            }

            foreach (var (material, mesh) in looks)
            {
                if (mesh.IsEmpty) continue;
                string name = material != null ? $"Overgrowth_{material.name}" : "Overgrowth";
                CreatePart(name, _assets.Track(mesh.ToMesh(name)), material, parent);
            }
        }

        // A tile's slab: its floor's thickness under it, reaching down to the lowest floor beside
        // it in a room with raised or sunken floor (ElevationGeometryBuilder.FloorSpan).
        private void AddFloorTile(LevelLayout layout, BoxMeshBuilder floors, Vector2Int t, RoomStructure structure)
        {
            float tile = layout.TileSize, floor = _settings.FloorThickness;
            (float bottom, float top) = structure != null && structure.HasElevation
                ? ElevationGeometryBuilder.FloorSpan(structure, t, floor)
                : (-floor, 0f);
            floors.AddBox(layout.TileToLocal(t) + Vector3.up * ((bottom + top) * 0.5f), new Vector3(tile, top - bottom, tile));
        }

        // The interior prefab's origin goes on the room's south-west floor corner, or, for a
        // landmark placed turned, on the corner LandmarkTurn names, turned the same way.
        private static void PlaceLandmark(LevelLayout layout, LevelRoom room, Transform parent)
        {
            if (room.Landmark.Prefab == null) return;
            Vector2Int min = room.Footprint.Bounds.min;
            Vector2 offset = LandmarkTurn.PrefabOffset(room.Landmark.SizeTiles, room.LandmarkTurns);
            Vector3 corner = new Vector3(min.x + offset.x, 0f, min.y + offset.y) * layout.TileSize;
            Quaternion turn = Quaternion.Euler(0f, LandmarkTurn.Yaw(room.LandmarkTurns), 0f);
            Object.Instantiate(room.Landmark.Prefab, parent.TransformPoint(corner), parent.rotation * turn, parent);
        }

        // Pieces short enough to shoot over go into `lowCover`, drawn in their own material, so
        // low and full-height cover read apart at a glance.
        private void AddStructure(LevelLayout layout, LevelRoom room, BoxMeshBuilder boxes, BoxMeshBuilder lowCover, Transform parent)
        {
            float tile = layout.TileSize;
            float height = room.WallHeight;

            foreach (StructurePillar pillar in room.Structure.Pillars)
            {
                // A pillar stands on the lowest floor round it, so it never floats over a ledge,
                // and reaches the ceiling (or, broken, its height above its own floor).
                float floor = LowestFloorAround(layout, pillar.Position);
                float own   = layout.FloorHeight(new Vector2Int(Mathf.FloorToInt(pillar.Position.x), Mathf.FloorToInt(pillar.Position.y)));
                Vector3 foot = new Vector3(pillar.Position.x * tile, floor, pillar.Position.y * tile);
                float h = pillar.Height > 0f ? Mathf.Min(pillar.Height + own - floor, height - floor) : height - floor;
                if (pillar.Prefab != null)
                {
                    // Pillar prefabs are 1 m wide and 1 m tall: stretched to this pillar, one
                    // column prefab fits every room height, broken columns included.
                    GameObject column = Object.Instantiate(pillar.Prefab, parent.TransformPoint(foot), parent.rotation, parent);
                    column.transform.localScale = Vector3.Scale(column.transform.localScale, new Vector3(pillar.Width, h, pillar.Width));
                    continue;
                }
                (CoverHeights.IsLow(h) ? lowCover : boxes).AddBox(foot + Vector3.up * (h * 0.5f), new Vector3(pillar.Width, h, pillar.Width));
            }

            // Inner walls stand centred on the edge between their two tiles.
            float thickness = _settings.WallThickness;
            // On a ledge, an inner wall stands from the lower floor to its height above the higher.
            foreach (PartitionEdge edge in room.Structure.Partitions)
            {
                float h = edge.Height > 0f ? Mathf.Min(edge.Height, height) : height;
                (float low, float high) = FloorsEither(layout, edge);
                float top = edge.Height > 0f ? Mathf.Min(high + h, height) : height;
                Vector3 centre = (layout.TileToLocal(edge.Tile) + layout.TileToLocal(edge.Other)) * 0.5f + Vector3.up * ((low + top) * 0.5f);
                Vector3 size = edge.Side.x != 0 ? new Vector3(thickness, top - low, tile + thickness) : new Vector3(tile + thickness, top - low, thickness);
                (CoverHeights.IsLow(h) ? lowCover : boxes).AddBox(centre, size);
            }

            // A diagonal wall stands corner to corner across its tile, a little longer than the
            // diagonal so the pieces of a run, and a run and the wall it meets, close up.
            foreach (DiagonalWall wall in room.Structure.Diagonals)
            {
                float floor = layout.FloorHeight(wall.Tile);
                float h = wall.Height > 0f ? Mathf.Min(wall.Height, height - floor) : height - floor;
                Vector2 middle = (wall.From + wall.To) * 0.5f, along = wall.To - wall.From;
                Vector3 centre = new Vector3(middle.x * tile, floor + h * 0.5f, middle.y * tile);
                var size = new Vector3(thickness, h, along.magnitude * tile + thickness);
                (CoverHeights.IsLow(h) ? lowCover : boxes).AddBox(centre, size, Quaternion.LookRotation(new Vector3(along.x, 0f, along.y)));
            }

            // A doorway through an inner wall: the gap is left, with a lintel over it.
            foreach (PartitionEdge opening in room.Structure.Openings)
            {
                if (opening.Height <= 0f || opening.Height >= height) continue;
                if (opening.HasDoor) HangDoor(layout, room, opening, parent);

                float bottom = FloorsEither(layout, opening).high + opening.Height;
                if (bottom >= height) continue;
                float lintel = height - bottom;
                Vector3 centre = (layout.TileToLocal(opening.Tile) + layout.TileToLocal(opening.Other)) * 0.5f + Vector3.up * (bottom + lintel * 0.5f);
                Vector3 size = opening.Side.x != 0 ? new Vector3(thickness, lintel, tile + thickness) : new Vector3(tile + thickness, lintel, thickness);
                boxes.AddBox(centre, size);
            }
        }

        // An interactable door in an inner doorway, on the floor, +Z along the wall's normal.
        private void HangDoor(LevelLayout layout, LevelRoom room, PartitionEdge opening, Transform parent)
        {
            GameObject prefab = _settings.InteriorDoorPrefabFor(room);
            if (prefab == null) return;

            Vector3 local = (layout.TileToLocal(opening.Tile) + layout.TileToLocal(opening.Other)) * 0.5f + Vector3.up * FloorsEither(layout, opening).low;
            var outward = new Vector3(opening.Side.x, 0f, opening.Side.y);
            Object.Instantiate(prefab, parent.TransformPoint(local), parent.rotation * Quaternion.LookRotation(outward), parent);
        }

        // The floors on the two sides of an inner wall, lower first.
        private static (float low, float high) FloorsEither(LevelLayout layout, PartitionEdge edge)
        {
            float a = layout.FloorHeight(edge.Tile), b = layout.FloorHeight(edge.Other);
            return (Mathf.Min(a, b), Mathf.Max(a, b));
        }

        // The lowest floor among the tiles touching a point (a tile's middle, or a corner where four meet).
        private static float LowestFloorAround(LevelLayout layout, Vector2 point)
        {
            float lowest = float.MaxValue;
            for (int dx = -1; dx <= 1; dx += 2)
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    var tile = new Vector2Int(Mathf.FloorToInt(point.x + dx * 0.49f), Mathf.FloorToInt(point.y + dy * 0.49f));
                    if (layout.RoomAt(tile) != null) lowest = Mathf.Min(lowest, layout.FloorHeight(tile));
                }
            return lowest == float.MaxValue ? 0f : lowest;
        }

        private void PlaceDoors(LevelLayout layout, Transform parent)
        {
            foreach (LevelDoorway doorway in layout.Doorways)
            {
                if (!doorway.HasGate) continue;

                bool objectiveGated = ObjectiveDoorPrefab != null && ObjectiveGated.Contains(doorway.Connection);
                GameObject prefab = objectiveGated ? ObjectiveDoorPrefab : _settings.DoorPrefabFor(doorway.Connection, doorway.Room, LeadsToStealthRoom(layout, doorway.Connection));
                if (prefab == null) continue;

                Pose pose = DoorwayPose(layout, doorway, parent);
                GameObject door = Object.Instantiate(prefab, pose.position, pose.rotation, parent);
                if (objectiveGated) ObjectiveDoors.Add(door);
                else if (doorway.Connection.IsGate) Gates[doorway.Connection] = door;

                // The door sits at the end nearer Start; a one-way door opens from the corridor
                // side, i.e. for someone arriving from the deeper room.
                if (doorway.Connection.OneWay) MakeOneWay(layout, doorway, door, pose.forward);
            }
        }

        private static bool LeadsToStealthRoom(LevelLayout layout, MapConnection connection) =>
            IsStealth(layout, connection.A) || IsStealth(layout, connection.B);

        private static bool IsStealth(LevelLayout layout, int roomId) =>
            layout.Rooms.TryGetValue(roomId, out LevelRoom room) && room.Node.Type == MapNodeType.Stealth;

        // Where a door goes: on the edge between the room tile and the corridor tile, on the
        // floor, +Z facing out of the room. World space.
        public static Pose DoorwayPose(LevelLayout layout, LevelDoorway doorway, Transform parent)
        {
            Vector3 position = (layout.TileToLocal(doorway.RoomTile) + layout.TileToLocal(doorway.OutsideTile)) * 0.5f;
            Vector3 outward  = new(doorway.Outward.x, 0f, doorway.Outward.y);
            return new Pose(parent.TransformPoint(position), parent.rotation * Quaternion.LookRotation(outward));
        }

        // Visual only: the kit pieces have no colliders to keep in sync with the boxes.
        private void PlaceWallKit(LevelLayout layout, LevelWallPlan wallPlan, Transform parent)
        {
            var holder = new GameObject("WallKit").transform;
            holder.SetParent(parent, false);
            foreach (WallKitPlacement piece in WallKitPlanner.Plan(layout, wallPlan, _settings.WallKitFor))
            {
                GameObject instance = Object.Instantiate(piece.Prefab, holder);
                instance.transform.SetLocalPositionAndRotation(piece.Position, piece.Rotation);
                instance.transform.localScale = piece.Scale;
            }
            // Hundreds of small pieces per level: batch them, they never move.
            StaticBatchingUtility.Combine(holder.gameObject);
        }

        private static void MakeOneWay(LevelLayout layout, LevelDoorway doorway, GameObject door, Vector3 openSide)
        {
            Door hinge = door.GetComponentInChildren<Door>();
            if (hinge != null)
                hinge.MakeOneWay(openSide);
            else
                layout.Warnings.Add($"One-way #{doorway.Connection.A}–#{doorway.Connection.B} is open both ways: its door prefab has no Door.");
        }

        private GameObject CreatePart(string name, BoxMeshBuilder builder, Material material, Transform parent, int layer = -1) =>
            builder.IsEmpty ? null : CreatePart(name, _assets.Track(builder.ToMesh(name)), material, parent, layer);

        // A static, solid piece of the level: drawn, collided with and (unless told otherwise) in the NavMesh.
        private GameObject CreatePart(string name, Mesh mesh, Material material, Transform parent, int layer = -1)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.layer = layer >= 0 ? layer : _settings.GeometryLayer;
            go.isStatic = true;
            go.transform.SetParent(parent, false);

            go.GetComponent<MeshFilter>().sharedMesh     = mesh;
            go.GetComponent<MeshCollider>().sharedMesh   = mesh;
            if (material != null) go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
