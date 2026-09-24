using System.Collections.Generic;

namespace CGD.Map
{
    // Checks a graph — generated or hand-edited — against structural sanity and the
    // settings' constraints, so edits that break a rule show up immediately.
    public static class MapGraphValidator
    {
        public static List<string> Validate(MapGraph graph, MapGraphAnalysis analysis, MapGenerationSettings settings)
        {
            var issues = new List<string>();

            CheckStructure(graph, analysis, issues);
            if (settings == null) return issues;

            CheckRuleCounts(graph, settings, issues);
            CheckNodes(graph, analysis, settings, issues);
            return issues;
        }

        private static void CheckStructure(MapGraph graph, MapGraphAnalysis analysis, List<string> issues)
        {
            ExpectCount(graph, MapNodeType.Start, issues);
            ExpectCount(graph, MapNodeType.Exit, issues);
            if (graph.CountOf(MapNodeType.Boss) == 0) issues.Add("No Boss node.");

            if (analysis.StartId != MapGraphAnalysis.Unreachable && analysis.ExitId != MapGraphAnalysis.Unreachable
                && !analysis.ExitReachable)
                issues.Add("Exit can't be reached from Start.");

            int unreachable = 0;
            foreach (MapNode node in graph.Nodes)
                if (!analysis.IsReachable(node.Id)) unreachable++;
            if (unreachable > 0 && analysis.StartId != MapGraphAnalysis.Unreachable)
                issues.Add($"{unreachable} node(s) can't be reached from Start.");
        }

        private static void ExpectCount(MapGraph graph, MapNodeType type, List<string> issues)
        {
            int count = graph.CountOf(type);
            if (count != 1) issues.Add($"Expected exactly one {type} node, found {count}.");
        }

        private static void CheckRuleCounts(MapGraph graph, MapGenerationSettings settings, List<string> issues)
        {
            foreach (MapNodeTypeRule rule in settings.NodeRules)
            {
                if (MapGenerationSettings.IsStructural(rule.Type))
                {
                    issues.Add($"Rule for {rule.Type} is ignored — Start, Boss and Exit are placed structurally.");
                    continue;
                }

                int count = graph.CountOf(rule.Type);
                if (count < rule.Min) issues.Add($"{rule.Type}: {count} below minimum {rule.Min}.");
                if (count > rule.Max) issues.Add($"{rule.Type}: {count} above maximum {rule.Max}.");
            }
        }

        private static void CheckNodes(MapGraph graph, MapGraphAnalysis analysis, MapGenerationSettings settings, List<string> issues)
        {
            var neighbors = new List<int>();

            foreach (MapNode node in graph.Nodes)
            {
                if (graph.Degree(node.Id) > settings.MaxConnectionsPerNode)
                    issues.Add($"#{node.Id} has more than {settings.MaxConnectionsPerNode} connections.");

                MapNodeTypeRule rule = settings.GetRule(node.Type);
                if (rule == null || MapGenerationSettings.IsStructural(node.Type)) continue;

                bool onMainPath = analysis.IsOnMainPath(node.Id);
                if (!rule.AllowsPlacement(onMainPath))
                    issues.Add($"#{node.Id} {node.Type} is {(onMainPath ? "on" : "off")} the main path (rule: {rule.Placement}).");

                if (analysis.IsReachable(node.Id) && !rule.AllowsDepth(analysis.Progress(node.Id)))
                    issues.Add($"#{node.Id} {node.Type} is outside its allowed depth.");

                if (!rule.AllowAdjacentSameType && HasNeighborOfType(graph, node, neighbors))
                    issues.Add($"#{node.Id} {node.Type} is next to another {node.Type}.");
            }
        }

        private static bool HasNeighborOfType(MapGraph graph, MapNode node, List<int> neighbors)
        {
            graph.GetNeighbors(node.Id, neighbors);
            foreach (int id in neighbors)
                if (graph.TryGetNode(id, out MapNode other) && other.Type == node.Type)
                    return true;
            return false;
        }
    }
}
