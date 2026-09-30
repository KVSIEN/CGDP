using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // The level as tiles: rooms, the corridors between them and their doorways. Plain data,
    // produced by LevelLayoutBuilder and turned into geometry by LevelGeometryBuilder.
    // Tile (x, y) covers world x and z from tile * TileSize, relative to the level's origin.
    public class LevelLayout
    {
        private readonly HashSet<Vector2Int> _walkable = new();
        private readonly HashSet<Vector2Int> _corridorTiles = new();
        private readonly Dictionary<Vector2Int, LevelRoom> _roomTiles = new();

        public LevelLayout(float tileSize) => TileSize = tileSize;

        public float TileSize { get; }

        public Dictionary<int, LevelRoom> Rooms     { get; } = new();
        public List<LevelDoorway>          Doorways  { get; } = new();
        public List<string>                Warnings  { get; } = new();

        public IEnumerable<Vector2Int> WalkableTiles => _walkable;

        public bool IsWalkable(Vector2Int tile) => _walkable.Contains(tile);
        public bool IsCorridor(Vector2Int tile) => _corridorTiles.Contains(tile);
        public LevelRoom RoomAt(Vector2Int tile) => _roomTiles.TryGetValue(tile, out LevelRoom room) ? room : null;

        public void AddRoom(LevelRoom room)
        {
            Rooms[room.Node.Id] = room;
            foreach (Vector2Int tile in room.Footprint.Tiles)
            {
                _walkable.Add(tile);
                _roomTiles[tile] = room;
            }
        }

        public void AddCorridor(IReadOnlyList<Vector2Int> tiles)
        {
            foreach (Vector2Int tile in tiles)
            {
                _walkable.Add(tile);
                _corridorTiles.Add(tile);
            }
        }

        // Centre of a tile, on the floor.
        public Vector3 TileToLocal(Vector2Int tile) => new((tile.x + 0.5f) * TileSize, 0f, (tile.y + 0.5f) * TileSize);

        public Vector3 RoomCenterLocal(LevelRoom room) => new(room.Anchor.x * TileSize, 0f, room.Anchor.y * TileSize);

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
