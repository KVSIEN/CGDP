using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A* over the tile grid for one-tile-wide corridors. Turns cost extra so corridors run in
    // long straight stretches rather than staircases.
    public class CorridorRouter
    {
        private const float TurnCost = 1.5f;

        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly RectInt _bounds;

        public CorridorRouter(RectInt bounds) => _bounds = bounds;

        // Tiles from start to goal inclusive, or null when no route exists. isPassable is
        // asked about every tile except start and goal themselves.
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isPassable)
        {
            var open     = new MinHeap();
            var cost     = new Dictionary<(Vector2Int, int), float>();
            var cameFrom = new Dictionary<(Vector2Int, int), (Vector2Int, int)>();

            // Direction -1 = "no direction yet", so the first step is never a turn.
            var startState = (start, -1);
            cost[startState] = 0f;
            open.Push(startState, Heuristic(start, goal));

            while (open.Count > 0)
            {
                var state = open.Pop();
                var (tile, dir) = state;
                if (tile == goal) return Reconstruct(cameFrom, state);

                float here = cost[state];
                for (int d = 0; d < Directions.Length; d++)
                {
                    Vector2Int next = tile + Directions[d];
                    if (!_bounds.Contains(next)) continue;
                    if (next != goal && !isPassable(next)) continue;

                    float step = 1f + (dir >= 0 && dir != d ? TurnCost : 0f);
                    var nextState = (next, d);
                    float nextCost = here + step;
                    if (cost.TryGetValue(nextState, out float known) && known <= nextCost) continue;

                    cost[nextState]     = nextCost;
                    cameFrom[nextState] = state;
                    open.Push(nextState, nextCost + Heuristic(next, goal));
                }
            }
            return null;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private static List<Vector2Int> Reconstruct(Dictionary<(Vector2Int, int), (Vector2Int, int)> cameFrom, (Vector2Int, int) end)
        {
            var path = new List<Vector2Int> { end.Item1 };
            while (cameFrom.TryGetValue(end, out var previous))
            {
                path.Add(previous.Item1);
                end = previous;
            }
            path.Reverse();
            return path;
        }

        // Binary heap on f-cost; .NET Standard 2.1 has no PriorityQueue.
        private class MinHeap
        {
            private readonly List<((Vector2Int, int) state, float priority)> _items = new();

            public int Count => _items.Count;

            public void Push((Vector2Int, int) state, float priority)
            {
                _items.Add((state, priority));
                int i = _items.Count - 1;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (_items[parent].priority <= _items[i].priority) break;
                    (_items[parent], _items[i]) = (_items[i], _items[parent]);
                    i = parent;
                }
            }

            public (Vector2Int, int) Pop()
            {
                var top = _items[0].state;
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _items.RemoveAt(last);

                int i = 0;
                while (true)
                {
                    int left = i * 2 + 1, right = left + 1, smallest = i;
                    if (left  < _items.Count && _items[left].priority  < _items[smallest].priority) smallest = left;
                    if (right < _items.Count && _items[right].priority < _items[smallest].priority) smallest = right;
                    if (smallest == i) break;
                    (_items[smallest], _items[i]) = (_items[i], _items[smallest]);
                    i = smallest;
                }
                return top;
            }
        }
    }
}
