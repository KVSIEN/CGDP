using UnityEngine;

namespace CGD.Level
{
    // Builds floors, walls and doors for a LevelLayout under a parent transform. A wall
    // goes on every edge between a walkable tile and a non-walkable one, so doorways are
    // simply the edges where a corridor tile meets a room tile.
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

            float tile      = layout.TileSize;
            float floor     = _settings.FloorThickness;
            float height    = _settings.WallHeight;
            float thickness = _settings.WallThickness;

            foreach (Vector2Int t in layout.WalkableTiles)
            {
                Vector3 centre = layout.TileToLocal(t);
                var floors = layout.IsCorridor(t) ? corridorFloors : roomFloors;
                floors.AddBox(centre + Vector3.down * (floor * 0.5f), new Vector3(tile, floor, tile));

                foreach (Vector2Int dir in Directions)
                {
                    if (layout.IsWalkable(t + dir)) continue;

                    // Sits just outside the tile and overlaps its neighbours at the corners,
                    // so walls meeting at a corner leave no gap.
                    Vector3 outward = new(dir.x, 0f, dir.y);
                    Vector3 position = centre + outward * ((tile + thickness) * 0.5f) + Vector3.up * (height * 0.5f);
                    Vector3 size = dir.x != 0
                        ? new Vector3(thickness, height, tile + thickness * 2f)
                        : new Vector3(tile + thickness * 2f, height, thickness);
                    walls.AddBox(position, size);
                }
            }

            CreatePart("RoomFloors",     roomFloors,     _settings.FloorMaterial,         parent);
            CreatePart("CorridorFloors", corridorFloors, _settings.CorridorFloorMaterial, parent);
            CreatePart("Walls",          walls,          _settings.WallMaterial,          parent);
            PlaceDoors(layout, parent);
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
