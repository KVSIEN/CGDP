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
                                    Seed seed, SeedVariants variants)
        {
            Layout      = layout;
            Content     = content;
            NodeSpacing = nodeSpacing;
            _seed     = seed;
            _variants = variants;
            Grid      = new MapGrid(layout.MaxSpread);
        }

        public MapLayoutSettings  Layout     { get; }
        public MapContentSettings Content    { get; }
        // Editor position per grid cell, for each node's Position.
        public Vector2            NodeSpacing { get; }
        public MapGraph           Graph      { get; } = new();
        public MapGrid            Grid       { get; }
        public List<MapSlot>      Slots      { get; } = new();
        public List<MapGatedArea> GatedAreas { get; } = new();
        public List<string>       Warnings   { get; } = new();

        public MapSlot GetSlot(int nodeId) => Slots.Find(s => s.NodeId == nodeId);

        // Each pass draws from its own stream, so rerolling one layer (a new variant)
        // leaves the numbers every other layer sees untouched.
        public RandomStream StreamFor(string layer) => StreamFor(_seed, _variants, layer);

        public static RandomStream StreamFor(Seed seed, SeedVariants variants, string layer) =>
            (variants != null ? variants.Resolve(seed, layer) : seed.Derive(layer)).Stream();

        public bool IsFull(int nodeId) => Graph.Degree(nodeId) >= Layout.MaxConnectionsPerNode;

        // Boss and Exit keep only their main path links, so the boss has a single entrance.
        public bool IsBossOrExit(int nodeId) =>
            Graph.TryGetNode(nodeId, out MapNode node) && (node.Type == MapNodeType.Boss || node.Type == MapNodeType.Exit);

        // Adds an extra link (beyond the tree the layout grows) unless a room is full or
        // it would bring the Boss closer to Start than MinBossDepth.
        public bool TryLink(int a, int b, ConnectionType type = ConnectionType.Normal)
        {
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

            foreach (MapNode node in Graph.Nodes)
            {
                bool onMainPath = analysis.IsOnMainPath(node.Id);
                Slots.Add(new MapSlot(node.Id, analysis.Depth(node.Id), analysis.Progress(node.Id), onMainPath,
                                      node.Type.IsStructural())
                {
                    IsDeadEnd = !onMainPath && Graph.Degree(node.Id) == 1
                });
            }
        }
    }
}
