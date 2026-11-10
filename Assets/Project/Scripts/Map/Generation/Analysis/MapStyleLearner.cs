using System;
using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Reads hand-made maps and works out the generation numbers that would grow maps like
    // them: how long the main path runs and how much it winds, how many side rooms there are
    // and how they branch, fork and loop, which gates there are, which room types appear
    // where and how often, and which categories sit beside which. Measurement only; turning
    // the numbers into settings assets is the editor's job.
    //
    // Ranges span what the examples did. With fewer than WidenBelow examples they're widened
    // by one each way, so new maps vary instead of cloning the one or two examples.
    public static class MapStyleLearner
    {
        private const int   WidenBelow       = 3;
        private const float DepthMargin      = 0.05f;
        private const float MinAffinitySample = 0.75f;
        private const float NeutralBand      = 0.15f;

        // Everything measured in one example map.
        private class Measures
        {
            public int  PathRooms, Optional, BossDepth, Spread, MaxDegree, HubBranches, Groups;
            public int  Loops, Shortcuts, OneWayShortcuts, Locked, Secret, TerminalLocks;
            public int  GateMinDepth = int.MaxValue;
            public int  PathTurns, PathSteps, BranchTurns, BranchSteps, Chains, Forks;
            public int  Direct, DirectEligible, MaxCombatRun;
            public bool AnyDirection;
            public readonly List<int> ChainLengths = new();
            public readonly List<int> TerminalCounts = new();
            public readonly Dictionary<MapNodeType, int> TypeCounts = new();
        }

        // One room of a non-structural type, for the type rules.
        private readonly struct Instance
        {
            public Instance(MapNodeType type, bool onMainPath, float progress, bool deadEnd)
            {
                Type = type; OnMainPath = onMainPath; Progress = progress; DeadEnd = deadEnd;
            }

            public MapNodeType Type { get; }
            public bool  OnMainPath { get; }
            public float Progress   { get; }
            public bool  DeadEnd    { get; }
        }

        // `spacings[i]` is the node spacing graphs[i] was laid out with (its grid cell size).
        // `content` decides which types count as fights for the pacing.
        public static LearnedMapStyle Learn(IReadOnlyList<MapGraph> graphs, IReadOnlyList<Vector2> spacings, MapContentSettings content)
        {
            var style     = new LearnedMapStyle();
            var measured  = new List<Measures>();
            var instances = new List<Instance>();
            var adjacent  = new HashSet<MapNodeType>();
            var closest   = new Dictionary<MapNodeType, int>();
            var categories = new Dictionary<RoomCategory, int>();
            var pairs      = new Dictionary<(RoomCategory, RoomCategory), int>();
            int categorisedLinks = 0;

            for (int i = 0; i < graphs.Count; i++)
            {
                MapGraph graph = graphs[i];
                var analysis = new MapGraphAnalysis(graph);
                if (!analysis.ExitReachable || graph.FindFirst(MapNodeType.Boss) == null)
                {
                    style.Notes.Add($"Skipped example {i + 1}: it needs a Start, a Boss and a reachable Exit.");
                    continue;
                }

                Measures m = Measure(graph, analysis, spacings[i], content);
                measured.Add(m);
                CollectTypes(graph, analysis, m, instances, adjacent, closest);
                categorisedLinks += CollectCategories(graph, categories, pairs);
            }

            style.MapCount = measured.Count;
            if (measured.Count == 0) return style;

            bool widen = measured.Count < WidenBelow;
            LearnLayout(style, measured, widen);
            LearnTypes(style, measured, instances, adjacent, closest, widen);
            LearnCategories(style, categories, pairs, categorisedLinks);
            Summarise(style, widen);
            return style;
        }

        // --- Measuring one map -----------------------------------------------------------

        private static Measures Measure(MapGraph graph, MapGraphAnalysis analysis, Vector2 spacing, MapContentSettings content)
        {
            var m = new Measures();
            var cells = new Dictionary<int, Vector2Int>();
            foreach (MapNode node in graph.Nodes)
                cells[node.Id] = new Vector2Int(Mathf.RoundToInt(node.Position.x / Mathf.Max(1f, spacing.x)),
                                                Mathf.RoundToInt(node.Position.y / Mathf.Max(1f, spacing.y)));

            IReadOnlyList<int> path = analysis.MainPath;   // Start … Boss, Exit
            m.PathRooms = Mathf.Max(0, path.Count - 3);
            m.BossDepth = MapGraphSearch.BossDistance(graph);

            // East paths never reach west of Start's column; any main path room there means
            // the map was laid out in any direction.
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int step = cells[path[i]] - cells[path[i - 1]];
                if (cells[path[i]].x < cells[path[0]].x) m.AnyDirection = true;
                if (i < 2) continue;
                m.PathSteps++;
                if (step != cells[path[i - 1]] - cells[path[i - 2]]) m.PathTurns++;
            }

            MeasureSpread(graph, analysis, cells, path, m);
            MeasureBranches(graph, analysis, cells, m);
            MeasureLinks(graph, analysis, cells, m);

            int run = 0;
            foreach (int id in path)
            {
                graph.TryGetNode(id, out MapNode node);
                run = content != null && content.Pacing.IsCombat(node.Type) ? run + 1 : 0;
                m.MaxCombatRun = Mathf.Max(m.MaxCombatRun, run);
            }
            return m;
        }

        // East maps: rows above and below Start. Any-direction maps: cells from the main path.
        private static void MeasureSpread(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, Vector2Int> cells,
                                          IReadOnlyList<int> path, Measures m)
        {
            Vector2Int start = cells[path[0]];
            foreach (MapNode node in graph.Nodes)
            {
                m.MaxDegree = Mathf.Max(m.MaxDegree, graph.Degree(node.Id));
                if (!analysis.IsReachable(node.Id)) continue;

                Vector2Int cell = cells[node.Id];
                if (!m.AnyDirection)
                {
                    m.Spread = Mathf.Max(m.Spread, Mathf.Abs(cell.y - start.y));
                    continue;
                }

                int nearest = int.MaxValue;
                foreach (int id in path)
                    nearest = Mathf.Min(nearest, Mathf.Abs(cell.x - cells[id].x) + Mathf.Abs(cell.y - cells[id].y));
                m.Spread = Mathf.Max(m.Spread, nearest);
            }
        }

        // Side rooms as chains: each off-path room hangs off its shallowest neighbour; a chain
        // runs from a dead end back to a fork or the main path. More chains than chains
        // leaving the path means forks.
        private static void MeasureBranches(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, Vector2Int> cells, Measures m)
        {
            var parent   = new Dictionary<int, int>();
            var children = new Dictionary<int, int>();
            var neighbors = new List<int>();
            var branchGroups = new HashSet<int>();

            foreach (MapNode node in graph.Nodes)
            {
                if (!analysis.IsReachable(node.Id) || analysis.IsOnMainPath(node.Id)) continue;
                m.Optional++;
                branchGroups.Add(analysis.BranchOf(node.Id));

                graph.GetNeighbors(node.Id, neighbors);
                int best = -1, bestDepth = int.MaxValue;
                foreach (int id in neighbors)
                {
                    int depth = analysis.Depth(id);
                    if (depth < 0 || depth >= analysis.Depth(node.Id) || depth >= bestDepth) continue;
                    best = id;
                    bestDepth = depth;
                }
                if (best < 0) continue;

                parent[node.Id] = best;
                children.TryGetValue(best, out int count);
                children[best] = count + 1;
                if (graph.FindFirst(MapNodeType.Start)?.Id == best) m.HubBranches++;
            }
            m.Groups = branchGroups.Count;

            int fromPath = 0;
            foreach (KeyValuePair<int, int> link in parent)
            {
                if (analysis.IsOnMainPath(link.Value)) fromPath++;
                else if (parent.TryGetValue(link.Value, out int grandparent))
                {
                    m.BranchSteps++;
                    if (cells[link.Key] - cells[link.Value] != cells[link.Value] - cells[grandparent]) m.BranchTurns++;
                }

                if (children.ContainsKey(link.Key)) continue;   // not a dead end
                m.Chains++;
                int length = 0, current = link.Key;
                while (true)
                {
                    length++;
                    int up = parent[current];
                    if (analysis.IsOnMainPath(up) || !parent.ContainsKey(up) || children[up] >= 2) break;
                    current = up;
                }
                m.ChainLengths.Add(length);
            }
            m.Forks = Mathf.Max(0, m.Chains - fromPath);
        }

        private static void MeasureLinks(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, Vector2Int> cells, Measures m)
        {
            int reachable = 0, plainLinks = 0;
            foreach (MapNode node in graph.Nodes)
                if (analysis.IsReachable(node.Id)) reachable++;

            foreach (MapConnection connection in graph.Connections)
            {
                if (connection.Type == ConnectionType.Shortcut)
                {
                    m.Shortcuts++;
                    if (connection.OneWay) m.OneWayShortcuts++;
                }
                else plainLinks++;

                if (connection.IsGate)
                {
                    if (connection.Type == ConnectionType.Locked) m.Locked++;
                    else m.Secret++;
                    m.GateMinDepth = Mathf.Min(m.GateMinDepth, Mathf.Max(0, Mathf.Min(analysis.Depth(connection.A), analysis.Depth(connection.B))));
                    if (connection.Type == ConnectionType.Locked && connection.Lock == MapLockKind.Terminals)
                    {
                        m.TerminalLocks++;
                        m.TerminalCounts.Add(connection.KeyNodeIds.Count);
                    }
                }

                Vector2Int gap = cells[connection.A] - cells[connection.B];
                if (connection.Type == ConnectionType.Normal && !connection.OneWay && Mathf.Abs(gap.x) + Mathf.Abs(gap.y) == 1)
                {
                    m.DirectEligible++;
                    if (connection.Direct) m.Direct++;
                }
            }
            m.Loops = Mathf.Max(0, plainLinks - (reachable - 1));
        }

        private static void CollectTypes(MapGraph graph, MapGraphAnalysis analysis, Measures m, List<Instance> instances,
                                         HashSet<MapNodeType> adjacent, Dictionary<MapNodeType, int> closest)
        {
            var mine = new List<MapNode>();
            foreach (MapNode node in graph.Nodes)
            {
                if (node.Type.IsStructural() || !analysis.IsReachable(node.Id)) continue;

                mine.Add(node);
                m.TypeCounts.TryGetValue(node.Type, out int count);
                m.TypeCounts[node.Type] = count + 1;
                bool onPath = analysis.IsOnMainPath(node.Id);
                instances.Add(new Instance(node.Type, onPath, analysis.Progress(node.Id), !onPath && graph.Degree(node.Id) == 1));
            }

            foreach (MapNode node in mine)
                foreach (KeyValuePair<int, int> reached in MapGraphSearch.Distances(graph, node.Id, includeShortcuts: true))
                {
                    if (reached.Value == 0 || !graph.TryGetNode(reached.Key, out MapNode other) || other.Type != node.Type) continue;
                    if (reached.Value == 1) adjacent.Add(node.Type);
                    closest[node.Type] = closest.TryGetValue(node.Type, out int known) ? Mathf.Min(known, reached.Value) : reached.Value;
                }
        }

        // Returns how many links join two categorised rooms.
        private static int CollectCategories(MapGraph graph, Dictionary<RoomCategory, int> categories,
                                             Dictionary<(RoomCategory, RoomCategory), int> pairs)
        {
            foreach (MapNode node in graph.Nodes)
                if (node.Category != RoomCategory.None)
                {
                    categories.TryGetValue(node.Category, out int count);
                    categories[node.Category] = count + 1;
                }

            int links = 0;
            foreach (MapConnection connection in graph.Connections)
            {
                if (!graph.TryGetNode(connection.A, out MapNode a) || !graph.TryGetNode(connection.B, out MapNode b)) continue;
                if (a.Category == RoomCategory.None || b.Category == RoomCategory.None) continue;

                links++;
                var key = PairKey(a.Category, b.Category);
                pairs.TryGetValue(key, out int count);
                pairs[key] = count + 1;
            }
            return links;
        }

        private static (RoomCategory, RoomCategory) PairKey(RoomCategory a, RoomCategory b) => a <= b ? (a, b) : (b, a);

        // --- Turning measures into numbers -------------------------------------------------

        private static void LearnLayout(LearnedMapStyle style, List<Measures> maps, bool widen)
        {
            int pathTurns = 0, pathSteps = 0, branchTurns = 0, branchSteps = 0, chains = 0, forks = 0;
            int shortcuts = 0, oneWay = 0, locked = 0, secret = 0, groups = 0, terminals = 0, direct = 0, eligible = 0;
            var chainLengths = new List<int>();
            var terminalCounts = new List<int>();

            style.MinBossDepth = int.MaxValue;
            style.GateMinDepth = -1;
            foreach (Measures m in maps)
            {
                pathTurns += m.PathTurns; pathSteps += m.PathSteps;
                branchTurns += m.BranchTurns; branchSteps += m.BranchSteps;
                chains += m.Chains; forks += m.Forks;
                shortcuts += m.Shortcuts; oneWay += m.OneWayShortcuts;
                locked += m.Locked; secret += m.Secret; groups += m.Groups; terminals += m.TerminalLocks;
                direct += m.Direct; eligible += m.DirectEligible;
                chainLengths.AddRange(m.ChainLengths);
                terminalCounts.AddRange(m.TerminalCounts);

                style.MinBossDepth          = Mathf.Min(style.MinBossDepth, m.BossDepth);
                style.MaxSpread             = Mathf.Max(style.MaxSpread, m.Spread);
                style.MaxConnectionsPerNode = Mathf.Max(style.MaxConnectionsPerNode, m.MaxDegree);
                style.MaxGates              = Mathf.Max(style.MaxGates, m.Locked + m.Secret);
                style.MaxCombatInARow       = Mathf.Max(style.MaxCombatInARow, m.MaxCombatRun);
                if (m.AnyDirection) style.Direction = MapPathDirection.Any;
                if (m.GateMinDepth != int.MaxValue)
                    style.GateMinDepth = style.GateMinDepth < 0 ? m.GateMinDepth : Mathf.Min(style.GateMinDepth, m.GateMinDepth);
            }

            style.MinBossDepth          = Mathf.Max(2, style.MinBossDepth == int.MaxValue ? 2 : style.MinBossDepth);
            style.MaxSpread             = Mathf.Max(1, style.MaxSpread);
            style.MaxConnectionsPerNode = Mathf.Max(2, style.MaxConnectionsPerNode);
            style.MaxCombatInARow       = Mathf.Max(1, style.MaxCombatInARow);

            style.OptionalRooms = Span(maps, m => m.Optional, widen, 0);
            style.PathLength    = Span(maps, m => m.PathRooms, widen, 1);
            style.HubBranches   = Span(maps, m => m.HubBranches, widen, 0);
            style.LoopCount     = Span(maps, m => m.Loops, widen, 0);
            style.ShortcutCount = Span(maps, m => m.Shortcuts, widen, 0);

            style.PathWinding = pathSteps > 0 ? Round((float)pathTurns / pathSteps) : 0f;
            if (chainLengths.Count > 0) style.BranchLength = Span(chainLengths, widen, 1);
            if (branchSteps > 0) style.BranchWinding = Round((float)branchTurns / branchSteps);
            if (chains > 0) style.ForkChance = Round((float)forks / chains);
            if (shortcuts > 0) style.OneWayShortcutChance = Round((float)oneWay / shortcuts);

            style.LockedChance = groups > 0 ? Round(Mathf.Clamp01((float)locked / groups)) : 0f;
            style.SecretChance = groups > locked ? Round(Mathf.Clamp01((float)secret / (groups - locked))) : 0f;
            if (locked > 0) style.TerminalLockChance = Round((float)terminals / locked);
            if (terminalCounts.Count > 0) style.TerminalCount = Span(terminalCounts, widen, 2);
            style.DirectDoorChance = eligible > 0 ? Round((float)direct / eligible) : 0f;
        }

        private static void LearnTypes(LearnedMapStyle style, List<Measures> maps, List<Instance> instances,
                                       HashSet<MapNodeType> adjacent, Dictionary<MapNodeType, int> closest, bool widen)
        {
            foreach (MapNodeType type in Enum.GetValues(typeof(MapNodeType)))
            {
                if (type.IsStructural()) continue;

                var rule = new LearnedTypeRule { Type = type };
                style.TypeRules.Add(rule);

                var counts = new List<int>();
                int total = 0;
                foreach (Measures m in maps)
                {
                    m.TypeCounts.TryGetValue(type, out int count);
                    counts.Add(count);
                    total += count;
                }
                rule.Seen = total > 0;
                if (!rule.Seen) continue;

                IntRange span = Span(counts, widen, 0);
                rule.Min    = span.Min;
                rule.Max    = span.Max;
                rule.Weight = Round(Mathf.Max(0.05f, (float)total / maps.Count));

                int onPath = 0, deadEnds = 0;
                float minDepth = 1f, maxDepth = 0f;
                foreach (Instance instance in instances)
                {
                    if (instance.Type != type) continue;
                    if (instance.OnMainPath) onPath++;
                    if (instance.DeadEnd) deadEnds++;
                    minDepth = Mathf.Min(minDepth, instance.Progress);
                    maxDepth = Mathf.Max(maxDepth, instance.Progress);
                }

                if (total >= 2)
                    rule.Placement = onPath == total ? MapPlacement.MainPathOnly : onPath == 0 ? MapPlacement.BranchOnly : MapPlacement.Anywhere;
                rule.MinDepth       = Round(Mathf.Clamp01(minDepth - DepthMargin));
                rule.MaxDepth       = Round(Mathf.Clamp01(maxDepth + DepthMargin));
                rule.PreferDeadEnds = total >= 2 && deadEnds >= total * 0.75f;
                rule.SeenAdjacent   = adjacent.Contains(type);
                rule.ClosestSpacing = closest.TryGetValue(type, out int spacing) ? spacing : 0;
            }
        }

        // Affinity = how often two categories really sat side by side, against how often
        // they would if rooms were shuffled at random. Pairs with too few chances to show
        // stay neutral.
        private static void LearnCategories(LearnedMapStyle style, Dictionary<RoomCategory, int> categories,
                                            Dictionary<(RoomCategory, RoomCategory), int> pairs, int links)
        {
            int rooms = 0;
            foreach (int count in categories.Values) rooms += count;
            if (rooms == 0 || links == 0) return;

            style.HasCategories = true;
            int categoryCount = Enum.GetValues(typeof(RoomCategory)).Length - 1;
            var zones = new Dictionary<ShipZone, int>();
            foreach (RoomCategory category in Enum.GetValues(typeof(RoomCategory)))
            {
                if (category == RoomCategory.None) continue;
                categories.TryGetValue(category, out int count);
                style.CategoryWeights[category] = Round((float)count / rooms * categoryCount);

                zones.TryGetValue(category.Zone(), out int inZone);
                zones[category.Zone()] = inZone + count;
            }
            foreach (KeyValuePair<ShipZone, int> zone in zones)
                style.ZoneShares[zone.Key] = Round((float)zone.Value / rooms * zones.Count);

            float sameSeen = 0f, sameExpected = 0f;
            foreach (KeyValuePair<RoomCategory, int> a in categories)
                foreach (KeyValuePair<RoomCategory, int> b in categories)
                {
                    if (a.Key > b.Key) continue;

                    float share    = (float)a.Value / rooms * b.Value / rooms;
                    float expected = links * (a.Key == b.Key ? share : 2f * share);
                    pairs.TryGetValue(PairKey(a.Key, b.Key), out int seen);

                    if (a.Key == b.Key)
                    {
                        sameSeen += seen;
                        sameExpected += expected;
                        continue;
                    }
                    if (expected < MinAffinitySample) continue;

                    float affinity = Round(Mathf.Clamp(seen / expected, 0.1f, 3f));
                    if (Mathf.Abs(affinity - 1f) >= NeutralBand) style.Affinities.Add((a.Key, b.Key, affinity));
                }

            if (sameExpected >= 1f) style.SameCategoryAffinity = Round(Mathf.Clamp(sameSeen / sameExpected, 0.1f, 3f));
        }

        private static void Summarise(LearnedMapStyle style, bool widen)
        {
            var notes = style.Notes;
            notes.Add($"Learned from {style.MapCount} map(s){(widen ? " — ranges widened by one each way, so new maps vary" : "")}.");
            notes.Add($"Main path {style.PathLength.Min}–{style.PathLength.Max} rooms, winding {style.PathWinding:0.00}, direction {style.Direction}; boss at least {style.MinBossDepth} away.");
            notes.Add($"Side rooms {style.OptionalRooms.Min}–{style.OptionalRooms.Max}, hub branches {style.HubBranches.Min}–{style.HubBranches.Max}, loops {style.LoopCount.Min}–{style.LoopCount.Max}, shortcuts {style.ShortcutCount.Min}–{style.ShortcutCount.Max}.");
            notes.Add($"Gates: locked {style.LockedChance:0.00}, secret {style.SecretChance:0.00}, at most {style.MaxGates}. Direct doors {style.DirectDoorChance:0.00}.");

            int seen = 0, unseen = 0;
            foreach (LearnedTypeRule rule in style.TypeRules)
                if (rule.Seen) seen++; else unseen++;
            notes.Add($"Room types: {seen} used, {unseen} never used (switched off).");
            notes.Add(style.HasCategories
                ? $"Categories: {style.Affinities.Count} neighbour pairs differ from chance."
                : "Categories: none set in the examples — the base category settings are kept.");
        }

        // --- Helpers -------------------------------------------------------------------

        private static IntRange Span(List<Measures> maps, Func<Measures, int> value, bool widen, int floor)
        {
            var values = new List<int>(maps.Count);
            foreach (Measures m in maps) values.Add(value(m));
            return Span(values, widen, floor);
        }

        private static IntRange Span(List<int> values, bool widen, int floor)
        {
            int min = int.MaxValue, max = int.MinValue;
            foreach (int v in values)
            {
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
            }
            if (widen) { min--; max++; }
            min = Mathf.Max(floor, min);
            return new IntRange(min, Mathf.Max(min, max));
        }

        private static float Round(float value) => Mathf.Round(value * 100f) / 100f;
    }
}
