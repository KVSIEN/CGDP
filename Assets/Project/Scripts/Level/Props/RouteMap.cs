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

        private readonly RoomStructure _structure;
        private readonly RectInt _bounds;
        private readonly HashSet<Vector2Int> _marked = new();
        // Steps per tile over the footprint's bounds; measured on first use and again after a
        // route is marked.
        private readonly int[] _distance;
        private bool _measured;
        private bool _hasRoutes;

        public RouteMap(RoomStructure structure)
        {
            _structure = structure;
            _bounds    = structure.Footprint.Bounds;
            _distance  = new int[_bounds.width * _bounds.height];
        }

        // A room with no route to measure from (no doorway) puts no limit on route distances.
        public bool HasRoutes
        {
            get
            {
                Measure();
                return _hasRoutes;
            }
        }

        // Steps from the tile to the nearest route: 0 on a route, Unreached where none leads.
        public int DistanceOf(Vector2Int tile)
        {
            Measure();
            return _structure.Contains(tile) ? _distance[IndexOf(tile)] : Unreached;
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
            if (_structure.Contains(tile) && _marked.Add(tile)) _measured = false;
        }

        private void Measure()
        {
            if (_measured) return;
            _measured  = true;
            _hasRoutes = false;
            System.Array.Fill(_distance, Unreached);

            var open = new Queue<Vector2Int>();
            foreach (Vector2Int tile in _structure.Footprint.Tiles)
                if (_structure.Has(tile, RoomTileTags.Walkway) || _marked.Contains(tile)) Reach(tile, 0, open);
            _hasRoutes = open.Count > 0;

            while (open.Count > 0)
            {
                Vector2Int tile = open.Dequeue();
                int next = _distance[IndexOf(tile)] + 1;
                foreach (Vector2Int side in GridSides.All)
                {
                    Vector2Int neighbour = tile + side;
                    if (!_structure.Contains(neighbour) || _distance[IndexOf(neighbour)] != Unreached) continue;
                    if (_structure.IsPartitioned(tile, neighbour) || _structure.Has(neighbour, RoomTileTags.Structure)) continue;
                    Reach(neighbour, next, open);
                }
            }
        }

        private void Reach(Vector2Int tile, int steps, Queue<Vector2Int> open)
        {
            int index = IndexOf(tile);
            if (_distance[index] != Unreached) return;
            _distance[index] = steps;
            open.Enqueue(tile);
        }

        private int IndexOf(Vector2Int tile) => (tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin);
    }
}
