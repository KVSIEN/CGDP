using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // Which grid cell each generated room occupies: x = column, y = row (lane), Start at
    // the origin. Rooms only ever link to rooms in nearby cells, and the grid is both
    // the editor layout and RoomPlacer's room cells, so linked rooms end up close by and
    // their corridors short.
    internal class MapGrid
    {
        private static readonly Vector2Int[] Directions =
            { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        private readonly Dictionary<int, Vector2Int> _cells = new();
        private readonly Dictionary<Vector2Int, int> _rooms = new();
        private readonly int _maxSpread;

        public MapGrid(int maxSpread)
        {
            _maxSpread = maxSpread;
        }

        public void Place(int nodeId, Vector2Int cell)
        {
            _cells[nodeId] = cell;
            _rooms[cell]   = nodeId;
        }

        public Vector2Int CellOf(int nodeId) => _cells[nodeId];

        public bool IsOpen(Vector2Int cell) =>
            !_rooms.ContainsKey(cell) && (_maxSpread == 0 || Mathf.Abs(cell.y) <= _maxSpread);

        public void CollectOpenNeighbors(Vector2Int cell, List<Vector2Int> results)
        {
            results.Clear();
            foreach (Vector2Int direction in Directions)
                if (IsOpen(cell + direction))
                    results.Add(cell + direction);
        }

        public void CollectNeighborRooms(Vector2Int cell, List<int> results)
        {
            results.Clear();
            foreach (Vector2Int direction in Directions)
                if (_rooms.TryGetValue(cell + direction, out int nodeId))
                    results.Add(nodeId);
        }

        public int Distance(int a, int b)
        {
            Vector2Int offset = _cells[a] - _cells[b];
            return Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
        }
    }
}
