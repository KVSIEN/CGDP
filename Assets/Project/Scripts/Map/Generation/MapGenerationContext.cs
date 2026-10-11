using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Shared state for the passes of one MapGenerator run.
    internal class MapGenerationContext
    {
        private readonly Seed         _seed;
        private readonly SeedVariants _variants;

        public MapGenerationContext(MapLayoutSettings layout, MapContentSettings content, Vector2 nodeSpacing,
                                    Seed seed, SeedVariants variants, MapRunTuning tuning)
        {
            Layout      = layout;
            Content     = content;
            Tuning      = tuning;
            NodeSpacing = nodeSpacing;
            _seed     = seed;
            _variants = variants;
            // Organic maps have no path to measure spread from; Compactness shapes them instead.
            Grid      = layout.Growth == MapGrowth.Organic
                ? new MapGrid(0)
                : new MapGrid(layout.MaxSpread, spreadAroundPath: layout.MainPath.Direction == MapPathDirection.Any);
        }

        public MapLayoutSettings  Layout     { get; }
        public MapContentSettings Content    { get; }
        // The run's modifiers, applied on top of the layout and content.
        public MapRunTuning       Tuning     { get; }
        // Editor position per grid cell, for each node's Position.
        public Vector2            NodeSpacing { get; }
        public MapGraph           Graph      { get; } = new();
        public MapGrid            Grid       { get; }
        public List<MapSlot>      Slots      { get; } = new();
        // Start to Boss, in order.
        public List<int>          MainPath   { get; } = new();
        public List<MapGatedArea> GatedAreas { get; } = new();
        // Locked main path connections the way to the Boss can't avoid, in path order.
        public List<MapConnection> ProgressionLocks { get; } = new();
        public List<string>       Warnings   { get; } = new();

        // How many progression locks lie between each room and Start. Extra links stay
        // within one zone, so nothing ever opens a way around a progression lock.
        private readonly Dictionary<int, int> _zones = new();

        public MapSlot GetSlot(int nodeId) => Slots.Find(s => s.NodeId == nodeId);

        // Each pass draws from its own stream, so rerolling one layer (a new variant)
        // leaves the numbers every other layer sees untouched.
        public RandomStream StreamFor(string layer) => StreamFor(_seed, _variants, layer);

        public static RandomStream StreamFor(Seed seed, SeedVariants variants, string layer) =>
            (variants != null ? variants.Resolve(seed, layer) : seed.Derive(layer)).Stream();

        // A new room on `cell` (positioned for the editor by NodeSpacing), linked to
        // `linkFrom` unless that is -1. Returns its id.
        public int AddRoom(MapNodeType type, Vector2Int cell, int linkFrom)
        {
            MapNode node = Graph.AddNode(type, new Vector2(cell.x * NodeSpacing.x, cell.y * NodeSpacing.y));
            Grid.Place(node.Id, cell);
            if (linkFrom >= 0) Graph.Connect(linkFrom, node.Id);
            return node.Id;
        }

        public bool IsFull(int nodeId) => Graph.Degree(nodeId) >= Layout.MaxConnectionsPerNode;

        // Boss and Exit keep only their main path links, so the boss has a single entrance.
        public bool IsBossOrExit(int nodeId) =>
            Graph.TryGetNode(nodeId, out MapNode node) && (node.Type == MapNodeType.Boss || node.Type == MapNodeType.Exit);

        public void SetZone(int nodeId, int zone) => _zones[nodeId] = zone;

        private int ZoneOf(int nodeId) => _zones.TryGetValue(nodeId, out int zone) ? zone : 0;

        // Adds an extra link (beyond the tree the layout grows) unless a room is full, it
        // would cross a progression lock, or it would bring the Boss closer to Start than
        // MinBossDepth.
        public bool TryLink(int a, int b, ConnectionType type = ConnectionType.Normal)
        {
            if (ZoneOf(a) != ZoneOf(b)) return false;
            if (IsFull(a) || IsFull(b) || !Graph.Connect(a, b, type)) return false;
            if (MapGraphSearch.BossDistance(Graph) >= Layout.MinBossDepth) return true;

            Graph.Disconnect(a, b);
            return false;
        }

        // Reads each room's depth, progress and place on the main path from the finished
        // layout, so the passes after it see exactly what MapGraphAnalysis — and so the
        // validator and the editor — will.
        public void DeriveSlots()
        {
            var analysis = new MapGraphAnalysis(Graph);
            Slots.Clear();
            MainPath.Clear();
            MainPath.AddRange(analysis.MainPath);

            foreach (MapNode node in Graph.Nodes)
            {
                bool onMainPath = analysis.IsOnMainPath(node.Id);
                Slots.Add(new MapSlot(node.Id, analysis.Depth(node.Id), analysis.Progress(node.Id), onMainPath,
                                      node.Type.IsStructural())
                {
                    IsDeadEnd     = !onMainPath && Graph.Degree(node.Id) == 1,
                    FreeNeighbors = Grid.Contains(node.Id) ? Grid.FreeNeighborCount(node.Id) : 0,
                    MainPathIndex = MainPath.IndexOf(node.Id),
                });
            }
        }
    }
}
