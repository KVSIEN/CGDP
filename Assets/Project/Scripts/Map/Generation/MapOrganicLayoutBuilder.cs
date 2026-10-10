using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Organic growth: rooms spread out from Start one at a time, each on a free cell beside
    // a room already placed (Compactness decides whether they fill gaps or push outward),
    // and link to that room — and, by Intersection Chance, to every other room they land
    // beside, so the map grows as a web. When it's full, the Boss goes beside the room
    // farthest from Start (counting every link) that is at least Min Boss Depth away, with
    // the Exit behind it, so the Boss keeps a single entrance and the route to it emerges
    // rather than being laid down first. The passes after this read the main path and
    // branches from the finished graph, so they work unchanged.
    internal class MapOrganicLayoutBuilder
    {
        private const int PlacementAttempts = 64;
        // Candidate spots compared per new room; Compactness picks the most or least crowded.
        private const int Samples = 3;
        private const int NoRoom = -1;

        private readonly MapGenerationContext _context;
        private readonly RandomStream         _random;
        private readonly List<int>            _rooms     = new();
        private readonly List<Vector2Int>     _openCells = new();
        private readonly List<int>            _neighbors = new();

        public MapOrganicLayoutBuilder(MapGenerationContext context, RandomStream random)
        {
            _context = context;
            _random  = random;
        }

        private MapGraph           Graph   => _context.Graph;
        private MapGrid            Grid    => _context.Grid;
        private MapLayoutSettings  Layout  => _context.Layout;
        private MapOrganicSettings Organic => Layout.Organic;

        public void Build()
        {
            // The same room budget as path-first growth: Start, the path's rooms and the
            // optional ones, then the Boss and Exit.
            int pathRooms = Layout.MainPathLength(Layout.MainPath.Length.Evaluate(_random));
            int optional  = Mathf.Max(0, Layout.OptionalRooms.Evaluate(_random) + _context.Tuning.ExtraOptionalRooms);
            int target    = 1 + pathRooms + optional;

            _rooms.Add(_context.AddRoom(MapNodeType.Start, Vector2Int.zero, NoRoom));
            int failures = 0;
            while (_rooms.Count < target && failures < PlacementAttempts)
                failures = TryGrow() ? 0 : failures + 1;

            if (_rooms.Count < target)
                _context.Warnings.Add($"Grew {_rooms.Count} of {target} rooms — rooms ran out of free sides; raise Max Connections Per Node.");

            PlaceBossAndExit();
        }

        private bool TryGrow()
        {
            bool wantCrowded = _random.Chance(Organic.Compactness);
            int parent = NoRoom, bestCrowd = 0;
            Vector2Int cell = default;

            for (int i = 0; i < Samples; i++)
            {
                int candidate = _random.Pick(_rooms);
                if (_context.IsFull(candidate)) continue;

                Grid.CollectOpenNeighbors(Grid.CellOf(candidate), _openCells);
                if (_openCells.Count == 0) continue;

                Vector2Int spot = _random.Pick(_openCells);
                int crowd = Crowd(spot);
                if (parent != NoRoom && (wantCrowded ? crowd <= bestCrowd : crowd >= bestCrowd)) continue;

                parent    = candidate;
                cell      = spot;
                bestCrowd = crowd;
            }
            if (parent == NoRoom) return false;

            int id = _context.AddRoom(_context.Content.FillType, cell, parent);
            _rooms.Add(id);
            Intersect(id, parent);
            return true;
        }

        // Rooms already beside a cell.
        private int Crowd(Vector2Int cell)
        {
            Grid.CollectNeighborRooms(cell, _neighbors);
            return _neighbors.Count;
        }

        private void Intersect(int id, int parent)
        {
            Grid.CollectNeighborRooms(Grid.CellOf(id), _neighbors);
            foreach (int other in _neighbors)
            {
                if (other == parent || !_random.Chance(Organic.IntersectionChance)) continue;
                if (_context.IsFull(id) || _context.IsFull(other)) continue;
                Graph.Connect(id, other);
            }
        }

        // The farthest room with space beside it for the Boss and, beyond that, the Exit.
        private void PlaceBossAndExit()
        {
            int start = _rooms[0];
            Dictionary<int, int> depth = MapGraphSearch.Distances(Graph, start, includeShortcuts: true);

            var order = new List<int>(_rooms);
            _random.Shuffle(order);   // ties between equally far rooms fall at random

            int anchor = NoRoom, anchorDepth = -1;
            Vector2Int bossCell = default, exitCell = default;
            foreach (int id in order)
            {
                if (_context.IsFull(id)) continue;
                int steps = depth.TryGetValue(id, out int known) ? known : 0;
                if (steps <= anchorDepth || !TryFindBossCells(id, out Vector2Int boss, out Vector2Int exit)) continue;

                anchor      = id;
                anchorDepth = steps;
                bossCell    = boss;
                exitCell    = exit;
            }

            if (anchor == NoRoom)
            {
                _context.Warnings.Add("No room had space beside it for the Boss and Exit.");
                return;
            }
            if (anchorDepth + 1 < Layout.MinBossDepth)
                _context.Warnings.Add($"The Boss is only {anchorDepth + 1} rooms from Start (Min Boss Depth {Layout.MinBossDepth}) — lower Intersection Chance or add rooms.");

            int bossId = _context.AddRoom(MapNodeType.Boss, bossCell, anchor);
            _context.AddRoom(MapNodeType.Exit, exitCell, bossId);
        }

        // A free cell beside `id` with another free cell beyond it.
        private bool TryFindBossCells(int id, out Vector2Int boss, out Vector2Int exit)
        {
            Grid.CollectOpenNeighbors(Grid.CellOf(id), _openCells);
            var options = new List<Vector2Int>(_openCells);
            _random.Shuffle(options);

            foreach (Vector2Int cell in options)
            {
                Grid.CollectOpenNeighbors(cell, _openCells);
                if (_openCells.Count == 0) continue;

                boss = cell;
                exit = _random.Pick(_openCells);
                return true;
            }
            boss = exit = default;
            return false;
        }
    }
}
