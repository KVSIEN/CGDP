using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // How far each floor tile of a room is from the routes people walk: the walkways between
    // its doorways and centre, plus the floor in front of props that mark a route (a service
    // door). Distances are steps over the floor plan, around inner walls and pillars but not
    // around props, so they hold still while a room is furnished. Concealment placements use
    // it to stand beside the routes (where people pass) or away from them (dead ends).
    internal sealed class RouteMap
    {
        public const int Unreached = int.MaxValue;

        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly RoomStructure _structure;
        private readonly HashSet<Vector2Int> _marked = new();
        private Dictionary<Vector2Int, int> _distance;

        public RouteMap(RoomStructure structure) => _structure = structure;

        // A room with no route to measure from (no doorway) puts no limit on route distances.
        public bool HasRoutes
        {
            get
            {
                Measure();
                return _distance.Count > 0;
            }
        }

        // Steps from the tile to the nearest route: 0 on a route, Unreached where none leads.
        public int DistanceOf(Vector2Int tile)
        {
            Measure();
            return _distance.TryGetValue(tile, out int steps) ? steps : Unreached;
        }

        // The nearest any tile of the rectangle comes to a route.
        public int DistanceOf(RectInt rect)
        {
            int nearest = Unreached;
            foreach (Vector2Int tile in rect.allPositionsWithin) nearest = Mathf.Min(nearest, DistanceOf(tile));
            return nearest;
        }

        public void Mark(Vector2Int tile)
        {
            if (!_structure.Footprint.Contains(tile) || !_marked.Add(tile)) return;
            _distance = null;
        }

        private void Measure()
        {
            if (_distance != null) return;
            _distance = new Dictionary<Vector2Int, int>();
            var open = new Queue<Vector2Int>();
            foreach (Vector2Int tile in _structure.Footprint.Tiles)
                if (_structure.Has(tile, RoomTileTags.Walkway) || _marked.Contains(tile)) Reach(tile, 0, open);

            while (open.Count > 0)
            {
                Vector2Int tile = open.Dequeue();
                int next = _distance[tile] + 1;
                foreach (Vector2Int side in Sides)
                {
                    Vector2Int neighbour = tile + side;
                    if (!_structure.Footprint.Contains(neighbour) || _distance.ContainsKey(neighbour)) continue;
                    if (_structure.IsPartitioned(tile, neighbour) || _structure.Has(neighbour, RoomTileTags.Structure)) continue;
                    Reach(neighbour, next, open);
                }
            }
        }

        private void Reach(Vector2Int tile, int steps, Queue<Vector2Int> open)
        {
            if (_distance.ContainsKey(tile)) return;
            _distance[tile] = steps;
            open.Enqueue(tile);
        }
    }
}
