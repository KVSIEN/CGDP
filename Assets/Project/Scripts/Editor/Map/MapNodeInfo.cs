using System.Collections.Generic;
using System.Text;
using CGD.Level;
using CGD.Map;
using UnityEngine;

namespace CGD.Editor
{
    // What the Map Graph window says about a node: the short lines on its card and the full
    // rundown shown while hovering it. Room places and enemy counts come from the level build
    // settings the window previews with (none = those parts are left out).
    public static class MapNodeInfo
    {
        // "Restaurant 2×1 · Commercial": the place the level gives it, its cells when it
        // spans more than one, and its category.
        public static string RoomLine(MapNode node, MapGraphEditorSession session, LevelBuildSettings build)
        {
            string category = node.Category != RoomCategory.None ? node.Category.ToString() : "no category";
            if (!TryGetRoom(node, session, build, out LevelRoom room) || room.Function == null) return category;

            Vector2Int cells = CellsOf(room, build);
            string size = cells.x * cells.y > 1 ? $" {cells.x}×{cells.y}" : "";
            return node.Category != RoomCategory.None ? $"{room.Function.DisplayName}{size} · {category}" : $"{room.Function.DisplayName}{size}";
        }

        // "d4 · branch 2 · dead end · gated".
        public static string RouteLine(MapNode node, MapGraphEditorSession session)
        {
            MapGraphAnalysis analysis = session.Analysis;
            if (!analysis.IsReachable(node.Id)) return "unreachable";

            var text = new StringBuilder($"d{analysis.Depth(node.Id)} · {Route(node.Id, analysis)}");
            if (IsDeadEnd(node, session.Graph)) text.Append(" · dead end");
            if (analysis.IsBehindGate(node.Id)) text.Append(" · gated");
            return text.ToString();
        }

        // "3 foes · 3/4 links · Commerce".
        public static string StatsLine(MapNode node, MapGraphEditorSession session, LevelBuildSettings build)
        {
            var parts = new List<string>();
            int foes = EnemyCount(node, build);
            if (foes > 0) parts.Add($"{foes} foes");
            parts.Add(LinksText(node, session) + " links");
            if (node.HasSection) parts.Add(SectionName(node, session));
            return string.Join(" · ", parts);
        }

        // Everything known about the node, one fact per line, for the hover panel.
        // `locks`: the graph's locked connections in order (L1 first); `plan`: the objective preview.
        public static void Describe(MapNode node, MapGraphEditorSession session, LevelBuildSettings build,
                                    IReadOnlyList<MapConnection> locks, IReadOnlyList<PlannedObjective> plan, List<string> lines)
        {
            lines.Clear();
            MapGraphAnalysis analysis = session.Analysis;
            MapGraph graph = session.Graph;

            string flags = (session.IsEdited(node) ? " · edited" : "") + (session.Asset.IsLocked(node.Id) ? " · locked" : "");
            lines.Add($"{node.Type} #{node.Id}{flags}");
            lines.Add($"Tier {node.EffectiveTier} · intensity {node.Intensity:0.00}");

            if (analysis.IsReachable(node.Id))
            {
                string route = $"Depth {analysis.Depth(node.Id)} · {analysis.Progress(node.Id):0%} of the way · {Route(node.Id, analysis)}";
                if (IsDeadEnd(node, graph)) route += " · dead end";
                if (analysis.IsBehindGate(node.Id)) route += " · behind a gate";
                lines.Add(route);
            }
            else lines.Add("Unreachable from Start");

            lines.Add(FactionText(node, session));
            lines.Add($"Section {(node.HasSection ? SectionName(node, session) : "none")} · zone {(node.Category != RoomCategory.None ? node.Category.Zone().ToString() : "none")} · category {(node.Category != RoomCategory.None ? node.Category.ToString() : "none")}");

            if (TryGetRoom(node, session, build, out LevelRoom room)) lines.Add(RoomText(room, build));

            int foes = EnemyCount(node, build);
            if (foes > 0 || node.Type.IsEncounter())
                lines.Add($"Enemies {foes} at build" + (node.Type.IsEncounter() ? " · encounter (waves when it starts)" : ""));

            lines.Add($"Links {LinksText(node, session)}: {Neighbours(node, graph)}");
            AddLocks(node, locks, lines);
            AddObjectives(node, plan, lines);

            IReadOnlyList<string> issues = session.IssuesAbout(node.Id);
            if (issues.Count == 0) return;
            lines.Add($"Issues ({issues.Count}):");
            foreach (string issue in issues) lines.Add("  • " + issue);
        }

        // Locks are numbered in graph order: the first locked connection is L1.
        public static string LockName(int lockIndex) => $"L{lockIndex + 1}";

        public static int EnemyCount(MapNode node, LevelBuildSettings build)
        {
            RoomContentRule rule = build != null ? build.RuleFor(node.Type) : null;
            return rule != null ? rule.EnemyCount.Lerp(node.Intensity) : 0;
        }

        private static bool TryGetRoom(MapNode node, MapGraphEditorSession session, LevelBuildSettings build, out LevelRoom room)
        {
            room = null;
            return build != null && session.Rooms.TryGetRoom(session, build, node.Id, out room);
        }

        // Main path rooms a loop can bypass, and side rooms every route must pass, are called out.
        private static string Route(int id, MapGraphAnalysis analysis)
        {
            bool required = analysis.IsRequired(id);
            if (analysis.IsOnMainPath(id)) return required ? "main path" : "main path (bypassable)";
            if (required) return "required";
            return $"branch {analysis.BranchOf(id) + 1}";
        }

        private static bool IsDeadEnd(MapNode node, MapGraph graph) => !node.Type.IsStructural() && graph.Degree(node.Id) == 1;

        // "3/4" against the layout's connection limit, or just "3".
        private static string LinksText(MapNode node, MapGraphEditorSession session)
        {
            int degree = session.Graph.Degree(node.Id);
            MapLayoutSettings layout = session.Asset.Layout;
            return layout != null ? $"{degree}/{layout.MaxConnectionsPerNode}" : degree.ToString();
        }

        private static string Neighbours(MapNode node, MapGraph graph)
        {
            var ids = new List<string>();
            foreach (MapConnection connection in graph.Connections)
            {
                if (connection.A != node.Id && connection.B != node.Id) continue;
                int other = connection.A == node.Id ? connection.B : connection.A;
                ids.Add(connection.Type == ConnectionType.Normal ? $"#{other}" : $"#{other} ({connection.Type.ToString().ToLowerInvariant()})");
            }
            return ids.Count > 0 ? string.Join(", ", ids) : "none";
        }

        private static string SectionName(MapNode node, MapGraphEditorSession session) =>
            session.Asset.Content?.SectionName(node.Section) ?? node.Section.ToString();

        private static string FactionName(MapGraphEditorSession session, int faction) =>
            session.Asset.Content?.FactionName(faction) ?? faction.ToString();

        private static string FactionText(MapNode node, MapGraphEditorSession session)
        {
            if (!node.HasFaction) return "Faction none";
            string text = $"Faction {FactionName(session, node.Faction)} (influence {node.FactionInfluence:0.00})";
            return node.HasBreachFaction ? $"{text} + {FactionName(session, node.BreachFaction)} breaching" : text;
        }

        // "Room Restaurant · 2×1 cells · 22×10 tiles · landmark · curved walls".
        private static string RoomText(LevelRoom room, LevelBuildSettings build)
        {
            Vector2Int cells = CellsOf(room, build);
            RectInt bounds = room.Footprint.Bounds;
            var text = new StringBuilder($"Room {(room.Function != null ? room.Function.DisplayName : "plain")}");
            text.Append($" · {cells.x}×{cells.y} cell{(cells.x * cells.y > 1 ? "s" : "")} · {bounds.width}×{bounds.height} tiles");
            if (room.Landmark != null) text.Append(" · landmark");
            if (room.Footprint.CurvedWalls) text.Append(" · curved walls");
            return text.ToString();
        }

        // A room spanning n cells is n cells wide less one corridor gap, so rounding up counts them.
        private static Vector2Int CellsOf(LevelRoom room, LevelBuildSettings build)
        {
            RectInt bounds = room.Footprint.Bounds;
            int cell = Mathf.Max(1, build.CellTiles);
            return new Vector2Int(Mathf.Max(1, Mathf.CeilToInt(bounds.width / (float)cell)),
                                  Mathf.Max(1, Mathf.CeilToInt(bounds.height / (float)cell)));
        }

        // "Holds L1 key" / "Holds L2 terminal 1", and "Door L1 (main lock) to #7" for its own locked links.
        private static void AddLocks(MapNode node, IReadOnlyList<MapConnection> locks, List<string> lines)
        {
            for (int i = 0; i < locks.Count; i++)
            {
                MapConnection connection = locks[i];
                int part = IndexOf(connection.KeyNodeIds, node.Id);
                if (part >= 0)
                    lines.Add(connection.UsesSingleKey ? $"Holds {LockName(i)} key" : $"Holds {LockName(i)} terminal {part + 1}");
                if (connection.A == node.Id || connection.B == node.Id)
                    lines.Add($"Locked door {LockName(i)} to #{(connection.A == node.Id ? connection.B : connection.A)}");
            }
        }

        private static void AddObjectives(MapNode node, IReadOnlyList<PlannedObjective> plan, List<string> lines)
        {
            for (int i = 0; i < plan.Count; i++)
            {
                int step = IndexOf(plan[i].NodeIds, node.Id);
                if (step >= 0)
                    lines.Add($"Objective {MapObjectivePreview.Letter(i)}{step + 1} ({(plan[i].IsMain ? "main" : "side")})");
            }
        }

        private static int IndexOf(IReadOnlyList<int> ids, int id)
        {
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] == id) return i;
            return -1;
        }
    }
}
