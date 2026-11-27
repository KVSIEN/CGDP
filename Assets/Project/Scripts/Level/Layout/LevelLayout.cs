using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // The level as tiles: rooms, the corridors between them and their doorways. Plain data,
    // produced by LevelLayoutBuilder and turned into geometry by LevelGeometryBuilder.
    // Tile (x, y) covers world x and z from tile * TileSize, relative to the level's origin.
    public class LevelLayout
    {
        private static readonly Vector2 TileMiddle = new(0.5f, 0.5f);

        private readonly HashSet<Vector2Int> _walkable = new();
        private readonly HashSet<Vector2Int> _corridorTiles = new();
        private readonly Dictionary<Vector2Int, LevelRoom> _roomTiles = new();

        public LevelLayout(float tileSize) => TileSize = tileSize;

        public float TileSize { get; }

        public Dictionary<int, LevelRoom> Rooms     { get; } = new();
        public List<LevelDoorway>          Doorways  { get; } = new();
        public List<string>                Warnings  { get; } = new();
        // The map director's plan for the map (MapDirector), set before any room is placed.
        public MapDirection Direction { get; private set; } = MapDirection.None;
        // What the director changed and found: its pacing pass, then its checks of the built rooms (BeatReview).
        public List<DirectorFinding> DirectorFindings { get; } = new();

        public void AttachDirection(MapDirection direction)
        {
            Direction = direction ?? MapDirection.None;
            DirectorFindings.AddRange(Direction.Findings);
        }

        public IEnumerable<Vector2Int> WalkableTiles => _walkable;

        public bool IsWalkable(Vector2Int tile) => _walkable.Contains(tile);
        public bool IsCorridor(Vector2Int tile) => _corridorTiles.Contains(tile);
        public LevelRoom RoomAt(Vector2Int tile) => _roomTiles.TryGetValue(tile, out LevelRoom room) ? room : null;

        // The room at a point in the level's own space (metres from its origin); null in a corridor.
        public LevelRoom RoomAtLocal(Vector3 local) =>
            RoomAt(new Vector2Int(Mathf.FloorToInt(local.x / TileSize), Mathf.FloorToInt(local.z / TileSize)));

        public void AddRoom(LevelRoom room)
        {
            Rooms[room.Node.Id] = room;
            foreach (Vector2Int tile in room.Footprint.Tiles)
            {
                _walkable.Add(tile);
                _roomTiles[tile] = room;
            }
        }

        // Shifts rooms that are already placed, together, before any corridor is built. All are
        // lifted before any is put down, so rooms moving into each other's old tiles stay whole.
        public void MoveRooms(IReadOnlyList<LevelRoom> rooms, Vector2Int offset)
        {
            foreach (LevelRoom room in rooms)
                foreach (Vector2Int tile in room.Footprint.Tiles)
                {
                    _walkable.Remove(tile);
                    _roomTiles.Remove(tile);
                }
            foreach (LevelRoom room in rooms) AddRoom(room.Translated(offset));
        }

        // The tiles of each connection's corridor.
        private readonly Dictionary<MapConnection, IReadOnlyList<Vector2Int>> _corridors = new();

        // Every corridor's tiles, from one doorway to the other.
        public IEnumerable<IReadOnlyList<Vector2Int>> Corridors => _corridors.Values;

        public IReadOnlyList<Vector2Int> CorridorOf(MapConnection connection) =>
            _corridors.TryGetValue(connection, out IReadOnlyList<Vector2Int> tiles) ? tiles : System.Array.Empty<Vector2Int>();

        public void AddCorridor(MapConnection connection, IReadOnlyList<Vector2Int> tiles)
        {
            _corridors[connection] = tiles;
            foreach (Vector2Int tile in tiles)
            {
                _walkable.Add(tile);
                _corridorTiles.Add(tile);
            }
        }

        // A doorway's middle on the floor and the way out of the room it faces, in the level's
        // space (LevelDoorway.Middle, Facing).
        public Vector3 DoorwayMiddle(LevelDoorway doorway) => new(doorway.Middle.x * TileSize, 0f, doorway.Middle.y * TileSize);
        public static Vector3 DoorwayOutward(LevelDoorway doorway) => new(doorway.Facing.x, 0f, doorway.Facing.y);

        // One tile's rectangle on the floor plan (x, z), in the level's space.
        public Rect TileLocalRect(Vector2Int tile) => new(tile.x * TileSize, tile.y * TileSize, TileSize, TileSize);

        // Centre of a tile, on the floor.
        public Vector3 TileToLocal(Vector2Int tile) => new((tile.x + 0.5f) * TileSize, 0f, (tile.y + 0.5f) * TileSize);

        // How high a tile's floor stands, in metres: its level in a room with raised or sunken
        // floor (RoomElevation), 0 for corridors and flat rooms. A flight of stairs counts as its
        // lower level.
        public float FloorHeight(Vector2Int tile)
        {
            RoomStructure structure = RoomAt(tile)?.Structure;
            return structure != null ? RoomElevation.HeightOf(structure.LevelOf(tile)) : 0f;
        }

        // How high the ground stands at a point on the floor plan (tile units), in metres: its
        // tile's floor plus any overgrowth lying on it (LevelRoom.Overgrowth).
        public float GroundHeight(Vector2 point)
        {
            var tile = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            OvergrowthField overgrowth = RoomAt(tile)?.Overgrowth;
            return FloorHeight(tile) + (overgrowth != null ? overgrowth.HeightAt(point) : 0f);
        }

        // The middle of a tile, standing on its ground: where things put on that tile go.
        public Vector3 TileToFloor(Vector2Int tile) => TileToLocal(tile) + Vector3.up * GroundHeight(tile + TileMiddle);

        // Where someone stands at a spot: on its tile's floor, or on the overpass deck over it.
        public Vector3 SpotToFloor(StandSpot spot)
        {
            Overpass deck = spot.OnDeck ? RoomAt(spot.Tile)?.Structure?.OverpassAbove(spot.Tile) : null;
            return deck != null ? TileToLocal(spot.Tile) + Vector3.up * RoomElevation.HeightOf(deck.Level) : TileToFloor(spot.Tile);
        }

        // The room's centre, on the ground of the tile it falls in.
        public Vector3 RoomCenterLocal(LevelRoom room) =>
            new(room.Anchor.x * TileSize, GroundHeight(room.Anchor), room.Anchor.y * TileSize);

        // The room's bounding rectangle on the floor plan (x, z), in the level's space.
        public Rect RoomLocalRect(LevelRoom room)
        {
            RectInt bounds = room.Footprint.Bounds;
            return Rect.MinMaxRect(bounds.xMin * TileSize, bounds.yMin * TileSize, bounds.xMax * TileSize, bounds.yMax * TileSize);
        }

        // World-space footprint of everything walkable, for fitting the world map to the level.
        public Rect LocalBounds()
        {
            if (_walkable.Count == 0) return Rect.zero;

            Vector2Int min = new(int.MaxValue, int.MaxValue), max = new(int.MinValue, int.MinValue);
            foreach (Vector2Int tile in _walkable)
            {
                min = Vector2Int.Min(min, tile);
                max = Vector2Int.Max(max, tile);
            }
            return Rect.MinMaxRect(min.x * TileSize, min.y * TileSize, (max.x + 1) * TileSize, (max.y + 1) * TileSize);
        }
    }
}
