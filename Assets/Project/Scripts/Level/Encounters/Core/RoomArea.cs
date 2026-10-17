using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // The part of a room that counts as "inside" for an encounter: its floor tiles minus
    // the ring around each doorway, so a room only seals once the player is clear of the
    // doors. Tiles are level tiles; positions are turned into tiles through the level's
    // transform.
    public class RoomArea
    {
        private readonly HashSet<Vector2Int> _tiles;
        private readonly float _tileSize;
        private readonly Matrix4x4 _worldToLevel;

        public RoomArea(IEnumerable<Vector2Int> tiles, float tileSize, Matrix4x4 worldToLevel)
        {
            _tiles        = new HashSet<Vector2Int>(tiles);
            _tileSize     = tileSize;
            _worldToLevel = worldToLevel;
        }

        // `doorTiles`: the room tiles just inside each doorway; every tile within one step of
        // them (diagonals included) is left out.
        public static RoomArea Inner(IEnumerable<Vector2Int> roomTiles, IEnumerable<Vector2Int> doorTiles, float tileSize, Matrix4x4 worldToLevel)
        {
            var near = new HashSet<Vector2Int>();
            foreach (Vector2Int door in doorTiles)
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        near.Add(door + new Vector2Int(x, y));

            var inner = new List<Vector2Int>();
            foreach (Vector2Int tile in roomTiles)
                if (!near.Contains(tile)) inner.Add(tile);
            return new RoomArea(inner, tileSize, worldToLevel);
        }

        public int  Count   => _tiles.Count;
        public bool IsEmpty => _tiles.Count == 0;
        public IEnumerable<Vector2Int> Tiles => _tiles;

        public bool Contains(Vector2Int tile) => _tiles.Contains(tile);

        public bool Contains(Vector3 world)
        {
            Vector3 local = _worldToLevel.MultiplyPoint3x4(world);
            return _tiles.Contains(new Vector2Int(Mathf.FloorToInt(local.x / _tileSize), Mathf.FloorToInt(local.z / _tileSize)));
        }
    }
}
