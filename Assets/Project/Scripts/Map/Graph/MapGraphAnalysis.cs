using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // Facts derived from a graph's shape: depth, the main path, which nodes are
    // required and which branch each optional node belongs to. Derived rather than
    // stored so hand edits can never leave them stale — build a new analysis after
    // the graph changes.
    //
    // All connection types count as traversable: a locked or secret link is still a
    // route once the player opens it. Depth and the main path ignore Shortcut links,
    // though — a shortcut is an optional skip, not the intended route, so it shouldn't
    // make the rooms it bypasses shallower or push them off the main path.
    public class MapGraphAnalysis
    {
        public const int Unreachable = -1;
        public const int MainPathBranch = -1;

        private readonly MapGraph                   _graph;
        private readonly Dictionary<int, int>       _depth;
        private readonly HashSet<int>               _reachable;
        private readonly Dictionary<int, int>       _branch   = new();
        private readonly HashSet<int>               _required = new();
        private readonly HashSet<int>               _mainPath = new();
        private readonly List<int>                  _mainPathOrder;
        private readonly List<int>                  _neighborBuffer = new();

        public MapGraphAnalysis(MapGraph graph)
        {
            _graph = graph;

            StartId = graph.FindFirst(MapNodeType.Start)?.Id ?? Unreachable;
            ExitId  = graph.FindFirst(MapNodeType.Exit)?.Id  ?? Unreachable;

            Dictionary<int, int> allRoutes = Distances(StartId, includeShortcuts: true);
            _reachable = new HashSet<int>(allRoutes.Keys);

            // Nodes only reachable through a shortcut fall back to their shortcut depth.
            _depth = Distances(StartId, includeShortcuts: false);
            foreach (KeyValuePair<int, int> entry in allRoutes)
                _depth.TryAdd(entry.Key, entry.Value);
            foreach (int depth in _depth.Values)
                if (depth > MaxDepth) MaxDepth = depth;

            _mainPathOrder = FindPath(StartId, ExitId, Unreachable, includeShortcuts: false);
            if (_mainPathOrder.Count == 0)
                _mainPathOrder = FindPath(StartId, ExitId, Unreachable, includeShortcuts: true);
            _mainPath.UnionWith(_mainPathOrder);
            ComputeRequired();
            ComputeBranches();
        }

        public int  StartId       { get; }
        public int  ExitId        { get; }
        public int  MaxDepth      { get; }
        public int  BranchCount   { get; private set; }
        public bool ExitReachable => _mainPathOrder.Count > 0;

        // Start → Exit along the fewest non-shortcut connections. Empty when Exit can't
        // be reached.
        public IReadOnlyList<int> MainPath => _mainPathOrder;

        public int Depth(int id) => _depth.TryGetValue(id, out int depth) ? depth : Unreachable;

        // Depth normalised so the node just before Exit (the boss, in a generated map)
        // sits at 1 — the same 0..1 scale MapNodeTypeRule depth ranges use.
        // Unreachable nodes report 0.
        public float Progress(int id)
        {
            int depth = Depth(id);
            if (depth == Unreachable) return 0f;

            int bossDepth = ExitReachable ? _mainPathOrder.Count - 2 : MaxDepth;
            return bossDepth <= 0 ? 0f : Mathf.Clamp01((float)depth / bossDepth);
        }

        public bool IsReachable(int id) => _reachable.Contains(id);

        public bool IsOnMainPath(int id) => _mainPath.Contains(id);

        // Every Start → Exit route passes through this node.
        public bool IsRequired(int id) => _required.Contains(id);

        // MainPathBranch for main path nodes, otherwise a 0-based branch index.
        // A branch is a group of off-path nodes connected to each other.
        public int BranchOf(int id) => _branch.TryGetValue(id, out int branch) ? branch : MainPathBranch;

        // Fewest-connections route, shortcuts included, inclusive of both ends. Empty
        // when there is none.
        public List<int> ShortestPath(int from, int to) => FindPath(from, to, Unreachable, includeShortcuts: true);

        private List<int> FindPath(int from, int to, int blocked, bool includeShortcuts)
        {
            var path = new List<int>();
            if (!_graph.TryGetNode(from, out _) || !_graph.TryGetNode(to, out _)) return path;
            if (from == blocked || to == blocked) return path;

            var previous = new Dictionary<int, int> { [from] = from };
            var frontier = new Queue<int>();
            frontier.Enqueue(from);

            while (frontier.Count > 0)
            {
                int current = frontier.Dequeue();
                if (current == to) break;

                CollectNeighbors(current, includeShortcuts);
                foreach (int next in _neighborBuffer)
                {
                    if (next == blocked || previous.ContainsKey(next)) continue;
                    previous[next] = current;
                    frontier.Enqueue(next);
                }
            }

            if (!previous.ContainsKey(to)) return path;

            for (int id = to; id != from; id = previous[id])
                path.Add(id);
            path.Add(from);
            path.Reverse();
            return path;
        }

        private Dictionary<int, int> Distances(int from, bool includeShortcuts)
        {
            var distances = new Dictionary<int, int>();
            if (from == Unreachable) return distances;

            var frontier = new Queue<int>();
            distances[from] = 0;
            frontier.Enqueue(from);

            while (frontier.Count > 0)
            {
                int current = frontier.Dequeue();

                CollectNeighbors(current, includeShortcuts);
                foreach (int next in _neighborBuffer)
                {
                    if (distances.ContainsKey(next)) continue;
                    distances[next] = distances[current] + 1;
                    frontier.Enqueue(next);
                }
            }
            return distances;
        }

        private void CollectNeighbors(int id, bool includeShortcuts)
        {
            _neighborBuffer.Clear();
            foreach (MapConnection connection in _graph.Connections)
            {
                if (!connection.Connects(id)) continue;
                if (!includeShortcuts && connection.Type == ConnectionType.Shortcut) continue;
                _neighborBuffer.Add(connection.Other(id));
            }
        }

        // Only main path nodes can be required: any node every route uses is on the
        // main path too. Graphs are small, so re-running a search per candidate
        // is simpler than a dominator tree and fast enough.
        private void ComputeRequired()
        {
            if (!ExitReachable) return;

            foreach (int id in _mainPathOrder)
            {
                bool isEndpoint = id == StartId || id == ExitId;
                if (isEndpoint || FindPath(StartId, ExitId, blocked: id, includeShortcuts: true).Count == 0)
                    _required.Add(id);
            }
        }

        private void ComputeBranches()
        {
            foreach (MapNode node in _graph.Nodes)
            {
                if (_mainPath.Contains(node.Id) || _branch.ContainsKey(node.Id)) continue;
                FloodBranch(node.Id, BranchCount++);
            }
        }

        private void FloodBranch(int seed, int branch)
        {
            var frontier = new Stack<int>();
            _branch[seed] = branch;
            frontier.Push(seed);

            while (frontier.Count > 0)
            {
                _graph.GetNeighbors(frontier.Pop(), _neighborBuffer);
                foreach (int next in _neighborBuffer)
                {
                    if (_mainPath.Contains(next) || _branch.ContainsKey(next)) continue;
                    _branch[next] = branch;
                    frontier.Push(next);
                }
            }
        }
    }
}
