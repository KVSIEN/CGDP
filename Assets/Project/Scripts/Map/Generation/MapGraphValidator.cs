using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Checks a graph — generated or hand-edited — against structural sanity and the
    // layout's and content's constraints, so edits that break a rule show up immediately.
    // Either settings asset may be null; its checks are then skipped. Pass the run's
    // modifiers (MapGenerator.PickModifiers) so rule counts are checked as they modify them.
    public static class MapGraphValidator
    {
        public static List<string> Validate(MapGraph graph, MapGraphAnalysis analysis,
                                            MapLayoutSettings layout, MapContentSettings content,
                                            MapRunTuning tuning = null)
        {
            tuning ??= MapRunTuning.None;
            var issues = new List<string>();

            CheckStructure(graph, analysis, issues);
            CheckLocks(graph, analysis, issues);
            CheckOneWays(graph, analysis, issues);

            if (layout != null)
            {
                CheckBossDepth(graph, layout, issues);
                CheckConnectionLimit(graph, layout, issues);
            }

            if (content != null)
            {
                CheckRuleCounts(graph, content, tuning, issues);
                CheckNodes(graph, analysis, content, issues);
                CheckPacing(graph, analysis, content.Pacing, issues);
                CheckFactions(graph, content, issues);
                foreach (MapGuarantee guarantee in content.Guarantees)
                {
                    if (guarantee == null || !guarantee.IsValid) continue;
                    switch (guarantee.Spot)
                    {
                        case MapGuaranteeSpot.WithinDepth:     CheckWithinDepth(graph, analysis, guarantee, issues); break;
                        case MapGuaranteeSpot.BehindEveryGate: CheckGatedRewards(graph, analysis, guarantee, issues); break;
                        case MapGuaranteeSpot.BeforeBoss:      CheckBossApproach(graph, analysis, guarantee, issues); break;
                    }
                }
            }
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

        // A locked door the player can never open is a soft-lock when the Exit is behind
        // it, and lost content otherwise — both are worth flagging.
        private static void CheckLocks(MapGraph graph, MapGraphAnalysis analysis, List<string> issues)
        {
            foreach (MapConnection connection in graph.Connections)
                if (connection.Type == ConnectionType.Locked && connection.UsesSingleKey && connection.KeyNodeIds.Count > 1)
                    issues.Add($"Locked #{connection.A}–#{connection.B} is a Keycard lock with {connection.KeyNodeIds.Count} key rooms; only one keycard is placed, in #{connection.KeyNodeIds[0]}.");

            if (analysis.StartId == MapGraphAnalysis.Unreachable) return;

            List<MapConnection> closed = MapGraphSearch.UnopenableLocks(graph, analysis.StartId);
            if (closed.Count == 0) return;

            // What the player can reach with every openable lock opened.
            var reachable = new HashSet<int>(MapGraphSearch.Distances(graph, analysis.StartId,
                c => c.Type != ConnectionType.Locked || !closed.Contains(c)).Keys);
            foreach (MapConnection connection in closed)
            {
                string gate = $"Locked #{connection.A}–#{connection.B}";
                string part = connection.UsesSingleKey ? "key" : "terminal";
                if (!connection.HasKey)
                {
                    issues.Add($"{gate} has no {part} room.");
                    continue;
                }

                foreach (int key in connection.KeyNodeIds)
                {
                    if (!graph.TryGetNode(key, out _))
                        issues.Add($"{gate}: its {part} room #{key} no longer exists.");
                    else if (!reachable.Contains(key))
                        issues.Add($"{gate} can't be opened: its {part} (#{key}) is behind it or behind another locked door.");
                }
            }
        }

        // A one-way door opens from its deep end, so the player must be able to get there
        // some other way — possibly through doors (locked or one-way) opened first. Locks are
        // treated as openable here; CheckLocks reports the ones that aren't.
        private static void CheckOneWays(MapGraph graph, MapGraphAnalysis analysis, List<string> issues)
        {
            if (analysis.StartId == MapGraphAnalysis.Unreachable) return;

            var closed = new List<MapConnection>();
            foreach (MapConnection connection in graph.Connections)
            {
                if (!connection.OneWay) continue;
                if (connection.IsGate)
                    issues.Add($"One-way #{connection.A}–#{connection.B} is also {connection.Type}; one-way only works on open passages.");
                else
                    closed.Add(connection);
            }

            bool openedAny = true;
            while (openedAny && closed.Count > 0)
            {
                Dictionary<int, int> reached = MapGraphSearch.Distances(graph, analysis.StartId, c => !closed.Contains(c));
                openedAny = closed.RemoveAll(c => reached.ContainsKey(DeepEnd(c, analysis))) > 0;
            }

            foreach (MapConnection connection in closed)
                issues.Add($"One-way #{connection.A}–#{connection.B} can't be opened: its far side (#{DeepEnd(connection, analysis)}) is only reachable through it.");
        }

        // Ties go to B, matching where the level builder puts the door (at A when A is no deeper).
        private static int DeepEnd(MapConnection connection, MapGraphAnalysis analysis) =>
            analysis.Depth(connection.A) > analysis.Depth(connection.B) ? connection.A : connection.B;

        private static void CheckBossDepth(MapGraph graph, MapLayoutSettings layout, List<string> issues)
        {
            int distance = MapGraphSearch.BossDistance(graph);
            if (distance != int.MaxValue && distance < layout.MinBossDepth)
                issues.Add($"Boss is {distance} connections from Start (minimum {layout.MinBossDepth}).");
        }

        private static void CheckConnectionLimit(MapGraph graph, MapLayoutSettings layout, List<string> issues)
        {
            foreach (MapNode node in graph.Nodes)
                if (graph.Degree(node.Id) > layout.MaxConnectionsPerNode)
                    issues.Add($"#{node.Id} has more than {layout.MaxConnectionsPerNode} connections.");
        }

        private static void CheckRuleCounts(MapGraph graph, MapContentSettings content, MapRunTuning tuning, List<string> issues)
        {
            foreach (MapNodeTypeRule rule in content.NodeRules)
            {
                if (rule.Type.IsStructural())
                {
                    issues.Add($"Rule for {rule.Type} is ignored — Start, Boss and Exit are placed by the layout.");
                    continue;
                }

                int count = graph.CountOf(rule.Type), min = tuning.Min(rule), max = tuning.Max(rule);
                if (count < min) issues.Add($"{rule.Type}: {count} below minimum {min}.");
                if (count > max) issues.Add($"{rule.Type}: {count} above maximum {max}.");
            }
        }

        // A room a Before Boss guarantee asked for is exempt from its rule's depth and
        // placement: the content asked for it there.
        private static void CheckNodes(MapGraph graph, MapGraphAnalysis analysis, MapContentSettings content, List<string> issues)
        {
            var neighbors = new List<int>();
            int bossApproach = BossApproachId(graph, analysis);

            foreach (MapNode node in graph.Nodes)
            {
                MapNodeTypeRule rule = content.GetRule(node.Type);
                if (rule == null || node.Type.IsStructural()) continue;

                bool exempt = node.Id == bossApproach && IsGuaranteedBeforeBoss(content, node.Type);
                bool onMainPath = analysis.IsOnMainPath(node.Id);
                if (!exempt && !rule.AllowsPlacement(onMainPath))
                    issues.Add($"#{node.Id} {node.Type} is {(onMainPath ? "on" : "off")} the main path (rule: {rule.Placement}).");

                if (!exempt && analysis.IsReachable(node.Id) && !rule.AllowsDepth(analysis.Progress(node.Id)))
                    issues.Add($"#{node.Id} {node.Type} is outside its allowed depth.");

                if (!rule.AllowAdjacentSameType && HasNeighborOfType(graph, node, neighbors))
                    issues.Add($"#{node.Id} {node.Type} is next to another {node.Type}.");

                if (rule.MinSpacing > 1 && HasSameTypeWithin(graph, node, rule.MinSpacing - 1))
                    issues.Add($"#{node.Id} {node.Type} is closer than {rule.MinSpacing} connections to another {node.Type}.");
            }
        }

        private static bool IsGuaranteedBeforeBoss(MapContentSettings content, MapNodeType type)
        {
            foreach (MapGuarantee guarantee in content.Guarantees)
                if (guarantee != null && guarantee.Spot == MapGuaranteeSpot.BeforeBoss && guarantee.Type == type) return true;
            return false;
        }

        private static bool HasSameTypeWithin(MapGraph graph, MapNode node, int distance)
        {
            foreach (var (id, steps) in MapGraphSearch.Distances(graph, node.Id, includeShortcuts: true))
                if (steps > 0 && steps <= distance && graph.TryGetNode(id, out MapNode other) && other.Type == node.Type)
                    return true;
            return false;
        }

        // Along the main path: fight runs and the room after an Elite.
        private static void CheckPacing(MapGraph graph, MapGraphAnalysis analysis, MapPacingSettings pacing, List<string> issues)
        {
            IReadOnlyList<int> path = analysis.MainPath;
            int run = 0;
            for (int i = 0; i < path.Count; i++)
            {
                if (!graph.TryGetNode(path[i], out MapNode node)) continue;
                bool combat = !node.Type.IsStructural() && pacing.IsCombat(node.Type);
                run = combat ? run + 1 : 0;

                if (pacing.MaxCombatInARow > 0 && run == pacing.MaxCombatInARow + 1)
                    issues.Add($"More than {pacing.MaxCombatInARow} fights in a row on the main path (up to #{node.Id}).");

                if (pacing.RestAfterTopTier && combat && i > 0 && graph.TryGetNode(path[i - 1], out MapNode before)
                    && before.EffectiveTier >= 3 && pacing.IsCombat(before.Type))
                    issues.Add($"#{node.Id} {node.Type} comes right after the tier-3 fight at #{before.Id}.");
            }
        }

        // With factions listed, every room is one of the ship's realities.
        private static void CheckFactions(MapGraph graph, MapContentSettings content, List<string> issues)
        {
            int count = content.Factions.Count;
            if (count == 0) return;

            foreach (MapNode node in graph.Nodes)
                if (!node.HasFaction || node.Faction >= count)
                    issues.Add($"#{node.Id} {node.Type} belongs to no faction.");
        }

        private static bool HasNeighborOfType(MapGraph graph, MapNode node, List<int> neighbors)
        {
            graph.GetNeighbors(node.Id, neighbors);
            foreach (int id in neighbors)
                if (graph.TryGetNode(id, out MapNode other) && other.Type == node.Type)
                    return true;
            return false;
        }

        private static void CheckWithinDepth(MapGraph graph, MapGraphAnalysis analysis, MapGuarantee guarantee, List<string> issues)
        {
            IntRange window = guarantee.Depth;
            int found = 0;
            foreach (MapNode node in graph.Nodes)
            {
                int depth = analysis.Depth(node.Id);
                if (node.Type == guarantee.Type && depth >= window.Min && depth <= window.Max) found++;
            }
            if (found < guarantee.Count)
                issues.Add($"{found} of {guarantee.Count} {guarantee.Type} {window.Min}–{window.Max} rooms from Start.");
        }

        private static void CheckGatedRewards(MapGraph graph, MapGraphAnalysis analysis, MapGuarantee guarantee, List<string> issues)
        {
            if (analysis.StartId == MapGraphAnalysis.Unreachable) return;

            MapNodeType type = guarantee.Type;
            foreach (MapConnection gate in graph.Connections)
            {
                if (!gate.IsGate) continue;

                // The area is whatever only this gate leads to. A gate with another way
                // around it closes nothing off, so there's no area to reward.
                Dictionary<int, int> around = MapGraphSearch.Distances(graph, analysis.StartId, c => c != gate);
                bool closesOffArea = false, rewarded = false;
                foreach (MapNode node in graph.Nodes)
                {
                    if (around.ContainsKey(node.Id) || !analysis.IsReachable(node.Id)) continue;
                    closesOffArea = true;
                    rewarded |= node.Type == type;
                }

                if (closesOffArea && !rewarded)
                    issues.Add($"Nothing behind {gate.Type} #{gate.A}–#{gate.B} is a {type}.");
            }
        }

        private static void CheckBossApproach(MapGraph graph, MapGraphAnalysis analysis, MapGuarantee guarantee, List<string> issues)
        {
            int id = BossApproachId(graph, analysis);
            if (id == MapGraphAnalysis.Unreachable || !graph.TryGetNode(id, out MapNode node) || node.Type.IsStructural()) return;
            if (node.Type != guarantee.Type)
                issues.Add($"The room before the Boss (#{id}) is {node.Type}, not {guarantee.Type}.");
        }

        // The main path room just before the Boss.
        private static int BossApproachId(MapGraph graph, MapGraphAnalysis analysis)
        {
            MapNode boss = graph.FindFirst(MapNodeType.Boss);
            if (boss == null) return MapGraphAnalysis.Unreachable;

            IReadOnlyList<int> path = analysis.MainPath;
            int index = -1;
            for (int i = 0; i < path.Count; i++)
                if (path[i] == boss.Id) index = i;
            return index > 0 ? path[index - 1] : MapGraphAnalysis.Unreachable;
        }
    }
}
