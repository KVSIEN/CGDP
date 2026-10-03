using System;
using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Adds the links that make a map more than a tree: loops between neighbouring rooms,
    // then shortcuts from shallow rooms to deeper ones nearby — some one-way, opened from
    // the deep end as a way back. Neither touches the Boss
    // or Exit, so the boss keeps a single entrance, and each is only kept when the Boss
    // stays at least MinBossDepth from Start.
    internal class MapLinkBuilder
    {
        private readonly MapGenerationContext _context;
        private readonly RandomStream         _random;

        public MapLinkBuilder(MapGenerationContext context, RandomStream random)
        {
            _context = context;
            _random  = random;
        }

        private MapGraph        Graph => _context.Graph;
        private MapLoopSettings Loops => _context.Layout.Loops;

        public void Build()
        {
            AddLinks(Loops.LoopCount.Evaluate(_random) + _context.Tuning.ExtraLoops, ConnectionType.Normal, "loops");
            AddLinks(Loops.ShortcutCount.Evaluate(_random), ConnectionType.Shortcut, "shortcuts");
        }

        private void AddLinks(int target, ConnectionType type, string label)
        {
            if (target <= 0) return;

            List<(int A, int B)> candidates = type == ConnectionType.Shortcut ? ShortcutCandidates() : LoopCandidates();
            _random.Shuffle(candidates);

            int placed = 0;
            foreach ((int a, int b) in candidates)
            {
                if (placed == target) break;
                if (!_context.TryLink(a, b, type)) continue;
                placed++;
                if (type == ConnectionType.Shortcut && _random.Chance(Loops.OneWayShortcutChance))
                    Graph.GetConnection(a, b).OneWay = true;
            }

            if (placed < target)
                _context.Warnings.Add($"Only placed {placed} of {target} {label} — rooms need neighbours with free connections, and Min Boss Depth may rule some out.");
        }

        private List<(int A, int B)> LoopCandidates() =>
            CollectPairs((a, b) => _context.Grid.Distance(a, b) == 1);

        // Depths ignore shortcuts, like MapGraphAnalysis, so each shortcut is measured
        // against the intended route rather than against earlier shortcuts.
        private List<(int A, int B)> ShortcutCandidates()
        {
            Dictionary<int, int> depths = MapGraphSearch.Distances(Graph, Graph.FindFirst(MapNodeType.Start).Id, includeShortcuts: false);
            int minGap = Loops.ShortcutMinSkip + 1;

            return CollectPairs((a, b) =>
            {
                int distance = _context.Grid.Distance(a, b);
                return distance <= Loops.ShortcutReach
                    && depths.TryGetValue(a, out int depthA) && depths.TryGetValue(b, out int depthB)
                    && Math.Abs(depthA - depthB) >= minGap;
            });
        }

        private List<(int A, int B)> CollectPairs(Func<int, int, bool> accepts)
        {
            var pairs = new List<(int A, int B)>();
            IReadOnlyList<MapNode> nodes = Graph.Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                int a = nodes[i].Id;
                if (_context.IsBossOrExit(a)) continue;

                for (int j = i + 1; j < nodes.Count; j++)
                {
                    int b = nodes[j].Id;
                    if (!_context.IsBossOrExit(b) && !Graph.AreConnected(a, b) && accepts(a, b))
                        pairs.Add((a, b));
                }
            }
            return pairs;
        }
    }
}
