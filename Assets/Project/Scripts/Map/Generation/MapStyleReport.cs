using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Generates a style for many seeds and sums up what it produces: sizes, room type counts,
    // structure, how often big-room types got space to spread into, faction territory and
    // how evenly it's split, faction mixes, sections, layouts and modifiers picked, and every generation warning or validation
    // issue with how often it came up. For tuning settings without paging through seeds
    // one at a time (Map Graph window › Analyze).
    public class MapStyleReport
    {
        private readonly Dictionary<MapNodeType, MapReportStat> _types = new();
        private readonly Dictionary<MapNodeType, (int rooms, int withSpace)> _space = new();
        private readonly Dictionary<string, int> _factionRooms = new();
        private readonly Dictionary<string, int> _sectionRooms = new();
        private readonly Dictionary<string, int> _factionMixes = new();
        private readonly Dictionary<string, int> _layouts = new();
        private readonly Dictionary<string, int> _modifiers = new();
        private readonly Dictionary<string, int> _problems = new();
        private int _totalRooms;

        public int Maps { get; private set; }
        public MapReportStat Rooms      { get; } = new();
        public MapReportStat MainPath   { get; } = new();
        public MapReportStat Branches   { get; } = new();
        public MapReportStat DeadEnds   { get; } = new();
        public MapReportStat Gates      { get; } = new();
        public MapReportStat Loops      { get; } = new();
        // Per map, % of rooms held by its biggest faction.
        public MapReportStat TopFaction { get; } = new();

        public IReadOnlyDictionary<MapNodeType, MapReportStat> TypeCounts => _types;
        // Problem text (ids replaced by #n) → maps it appeared in.
        public IReadOnlyDictionary<string, int> Problems => _problems;

        // Share of rooms of `type` with an empty grid cell beside them (0..1).
        public float SpaceRate(MapNodeType type) =>
            _space.TryGetValue(type, out var s) && s.rooms > 0 ? (float)s.withSpace / s.rooms : 0f;

        public static MapStyleReport Run(MapGenerationSettings settings, int seeds, int firstSeed = 1)
        {
            var report = new MapStyleReport();
            var generator = new MapGenerator(settings);
            for (int seed = firstSeed; seed < firstSeed + seeds; seed++)
                report.Add(generator.Generate(Seed.From(seed)), settings);
            return report;
        }

        private void Add(MapGenerationResult result, MapGenerationSettings settings)
        {
            MapGraph graph = result.Graph;
            var analysis = new MapGraphAnalysis(graph);
            MapContentSettings content = settings.Content;
            Maps++;

            Rooms.Add(graph.Nodes.Count);
            MainPath.Add(analysis.MainPath.Count);
            Branches.Add(analysis.BranchCount);
            Loops.Add(graph.Connections.Count - graph.Nodes.Count + 1);

            int gates = 0, deadEnds = 0;
            foreach (MapConnection connection in graph.Connections)
                if (connection.IsGate) gates++;
            Gates.Add(gates);

            var perType    = new Dictionary<MapNodeType, int>();
            var perFaction = new Dictionary<int, int>();
            var cells = new HashSet<Vector2Int>();
            foreach (MapNode node in graph.Nodes) cells.Add(CellOf(node, settings.NodeSpacing));

            foreach (MapNode node in graph.Nodes)
            {
                perType.TryGetValue(node.Type, out int count);
                perType[node.Type] = count + 1;
                if (!node.Type.IsStructural() && graph.Degree(node.Id) == 1) deadEnds++;

                _space.TryGetValue(node.Type, out var space);
                bool free = HasFreeNeighbor(CellOf(node, settings.NodeSpacing), cells);
                _space[node.Type] = (space.rooms + 1, space.withSpace + (free ? 1 : 0));

                if (node.HasFaction) Increment(_factionRooms, content.FactionName(node.Faction));
                perFaction.TryGetValue(node.Faction, out int held);
                perFaction[node.Faction] = held + 1;
                if (node.HasSection) Increment(_sectionRooms, content.SectionName(node.Section));
                _totalRooms++;
            }
            DeadEnds.Add(deadEnds);
            AddFactionSplit(perFaction, graph.Nodes.Count);
            Increment(_factionMixes, result.FactionMix != null ? result.FactionMix.DisplayName : "(none)");

            foreach (MapNodeType type in System.Enum.GetValues(typeof(MapNodeType)))
            {
                if (!_types.TryGetValue(type, out MapReportStat stat)) _types[type] = stat = new MapReportStat();
                perType.TryGetValue(type, out int count);
                stat.Add(count);
            }

            Increment(_layouts, result.Layout != null ? result.Layout.name : "(none)");
            if (result.Modifiers.Count == 0) Increment(_modifiers, "(none)");
            foreach (MapRunModifier modifier in result.Modifiers) Increment(_modifiers, modifier.DisplayName);

            // Each distinct problem counts once per map.
            var seen = new HashSet<string>();
            foreach (string warning in result.Warnings) seen.Add(Normalize(warning));
            foreach (string issue in MapGraphValidator.Validate(graph, analysis, result.Layout, content, new MapRunTuning(result.Modifiers)))
                seen.Add(Normalize(issue));
            foreach (string problem in seen) Increment(_problems, problem);
        }

        private void AddFactionSplit(Dictionary<int, int> perFaction, int rooms)
        {
            if (rooms == 0) return;
            int top = 0;
            foreach (var (faction, count) in perFaction)
                if (faction != MapNode.NoFaction && count > top) top = count;
            TopFaction.Add(Mathf.RoundToInt(100f * top / rooms));
        }

        public string ToText()
        {
            var text = new StringBuilder();
            text.AppendLine($"{Maps} maps — average (min–max)");
            text.AppendLine($"  Rooms        {Rooms}");
            text.AppendLine($"  Main path    {MainPath}");
            text.AppendLine($"  Branches     {Branches}");
            text.AppendLine($"  Dead ends    {DeadEnds}");
            text.AppendLine($"  Gates        {Gates}");
            text.AppendLine($"  Loops        {Loops}");

            text.AppendLine().AppendLine("Room types — count, and % with space beside them");
            foreach (var (type, stat) in _types)
            {
                if (stat.Max <= 0) continue;
                text.AppendLine($"  {type,-10} {stat,-20} {SpaceRate(type):P0} space");
            }

            AppendShares(text, "Faction territory (share of all rooms)", _factionRooms, _totalRooms);
            if (_factionRooms.Count > 0)
                text.AppendLine($"  Biggest faction  {TopFaction} % of a map's rooms");
            AppendShares(text, "Faction mixes picked", _factionMixes, Maps);
            AppendShares(text, "Sections (share of all rooms)", _sectionRooms, _totalRooms);
            AppendShares(text, "Layouts picked", _layouts, Maps);
            AppendShares(text, "Run modifiers (share of maps)", _modifiers, Maps);

            text.AppendLine().AppendLine(_problems.Count == 0 ? "No warnings or validation issues." : "Warnings and issues (share of maps)");
            var problems = new List<KeyValuePair<string, int>>(_problems);
            problems.Sort((a, b) => b.Value.CompareTo(a.Value));
            foreach (var (problem, count) in problems)
                text.AppendLine($"  {(float)count / Maps,5:P0}  {problem}");
            return text.ToString();
        }

        private static void AppendShares(StringBuilder text, string title, Dictionary<string, int> counts, int total)
        {
            if (counts.Count == 0 || total == 0) return;
            text.AppendLine().AppendLine(title);
            foreach (var (name, count) in counts)
                text.AppendLine($"  {name,-16} {(float)count / total:P0}");
        }

        private static Vector2Int CellOf(MapNode node, Vector2 spacing) => new(
            Mathf.RoundToInt(node.Position.x / Mathf.Max(1f, spacing.x)),
            Mathf.RoundToInt(node.Position.y / Mathf.Max(1f, spacing.y)));

        private static bool HasFreeNeighbor(Vector2Int cell, HashSet<Vector2Int> taken) =>
            !taken.Contains(cell + Vector2Int.right) || !taken.Contains(cell + Vector2Int.left)
            || !taken.Contains(cell + Vector2Int.up) || !taken.Contains(cell + Vector2Int.down);

        // Ids and counts vary per map; "#12" and "3 of 4" become "#n" and "n of n" so the
        // same problem groups together.
        private static string Normalize(string message) => Regex.Replace(message, @"\d+", "n");

        private static void Increment(Dictionary<string, int> counts, string key)
        {
            counts.TryGetValue(key, out int count);
            counts[key] = count + 1;
        }
    }
}
