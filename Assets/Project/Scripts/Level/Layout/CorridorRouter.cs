using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Algorithms;

namespace CGD.Level
{
    // A* over the tile grid for one-tile-wide corridors. Turns cost extra so corridors run in
    // long straight stretches rather than staircases.
    //
    // A search is a state of (tile, heading); the router keeps its cost and parent in flat arrays
    // over its bounds, reused between searches (a stamp says which entries belong to the current
    // one), because a level asks for dozens of routes. Not thread-safe.
    public class CorridorRouter
    {
        private const float TurnCost = 1.5f;
        private const int   Headings = 5;   // four directions, plus "no direction yet" for the first step

        private readonly RectInt _bounds;
        private readonly float[] _cost;
        private readonly int[]   _cameFrom;
        private readonly int[]   _stamp;
        // States waiting to be explored, cheapest f-cost first.
        private readonly MinHeap<int> _open = new();
        private int _search;

        public CorridorRouter(RectInt bounds)
        {
            _bounds   = bounds;
            int size  = Mathf.Max(0, bounds.width * bounds.height) * Headings;
            _cost     = new float[size];
            _cameFrom = new int[size];
            _stamp    = new int[size];
        }

        // Tiles from start to goal inclusive, or null when no route exists. isPassable is
        // asked about every tile except start and goal themselves.
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isPassable)
        {
            if (!_bounds.Contains(start)) return null;

            _search++;
            _open.Clear();

            // Heading 0 = "no direction yet", so the first step is never a turn.
            int startState = StateOf(start, 0);
            Visit(startState, 0f, -1);
            _open.Push(startState, Heuristic(start, goal));

            while (_open.Count > 0)
            {
                int state = _open.Pop();
                int heading = state % Headings;
                Vector2Int tile = TileOf(state / Headings);
                if (tile == goal) return Reconstruct(state);

                float here = _cost[state];
                for (int d = 0; d < GridNeighbours.Four.Length; d++)
                {
                    Vector2Int next = tile + GridNeighbours.Four[d];
                    if (!_bounds.Contains(next)) continue;
                    if (next != goal && !isPassable(next)) continue;

                    float step = 1f + (heading > 0 && heading != d + 1 ? TurnCost : 0f);
                    int nextState = StateOf(next, d + 1);
                    float nextCost = here + step;
                    if (_stamp[nextState] == _search && _cost[nextState] <= nextCost) continue;

                    Visit(nextState, nextCost, state);
                    _open.Push(nextState, nextCost + Heuristic(next, goal));
                }
            }
            return null;
        }

        private int StateOf(Vector2Int tile, int heading) =>
            ((tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin)) * Headings + heading;

        private Vector2Int TileOf(int cell) => new(_bounds.xMin + cell % _bounds.width, _bounds.yMin + cell / _bounds.width);

        private void Visit(int state, float cost, int from)
        {
            _stamp[state]    = _search;
            _cost[state]     = cost;
            _cameFrom[state] = from;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private List<Vector2Int> Reconstruct(int end)
        {
            var path = new List<Vector2Int>();
            for (int state = end; state >= 0; state = _cameFrom[state])
                path.Add(TileOf(state / Headings));
            path.Reverse();
            return path;
        }
    }
}
