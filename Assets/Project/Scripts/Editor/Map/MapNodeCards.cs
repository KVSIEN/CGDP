using System;
using System.Collections.Generic;
using CGD.Factions;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Draws everything on and around a node for MapGraphCanvas: the card itself (colour,
    // type stripe, intensity bar, text lines, lock icon, dimming), the badges and chips on
    // its edges (keys and terminals, factions, tier, issues, objective steps) and the hover
    // panel. The canvas owns where nodes are and hands over each node's screen rect.
    //
    // With Details off, a card shows its title and one line about the current view mode.
    public class MapNodeCards
    {
        private const float TitleHeight     = 16f;
        private const float LineHeight      = 12f;
        private const float ChipHeight      = 16f;
        private const float HoverLineHeight = 14f;

        // How far along each room's bottom edge the next key badge goes.
        private readonly Dictionary<int, float> _keyBadgeOffsets = new();
        private readonly List<string> _hoverLines = new();

        private Func<MapNode, Rect> _rectOf;
        // Locked connections in graph order (L1 first), and the objective preview.
        private IReadOnlyList<MapConnection>    _locks;
        private IReadOnlyList<PlannedObjective> _plan;

        private GUIStyle _titleStyle;
        private GUIStyle _detailStyle;
        private GUIStyle _badgeStyle;
        private GUIStyle _keyBadgeStyle;
        private GUIStyle _chipStyle;
        private GUIStyle _hoverStyle;

        // The cards, then what sits on their edges, so badges overlap neighbouring cards
        // rather than hide under them. Repaint only.
        public void Draw(MapGraphEditorSession session, Func<MapNode, Rect> rectOf,
                         IReadOnlyList<MapConnection> locks, IReadOnlyList<PlannedObjective> plan)
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsureStyles();
            _rectOf = rectOf;
            _locks  = locks;
            _plan   = plan;

            foreach (MapNode node in session.Graph.Nodes)
                DrawCard(node, session);

            DrawKeyBadges(session);
            DrawFactionChips(session);
            DrawTierAndIssueChips(session);
            DrawObjectiveBadges(session);
        }

        // Everything known about `node`, beside the cursor and kept inside `bounds`. Repaint only.
        public void DrawHoverPanel(MapGraphEditorSession session, MapNode node, Vector2 mouse, Rect bounds,
                                   IReadOnlyList<MapConnection> locks, IReadOnlyList<PlannedObjective> plan)
        {
            if (Event.current.type != EventType.Repaint || node == null) return;
            EnsureStyles();

            MapNodeInfo.Describe(node, session, session.Objectives.BuildSettings, locks, plan, _hoverLines);

            float width = 0f;
            for (int i = 0; i < _hoverLines.Count; i++)
            {
                _hoverStyle.fontStyle = i == 0 ? FontStyle.Bold : FontStyle.Normal;
                width = Mathf.Max(width, _hoverStyle.CalcSize(new GUIContent(_hoverLines[i])).x);
            }
            var size  = new Vector2(Mathf.Min(width, 460f) + 12f, _hoverLines.Count * HoverLineHeight + 10f);
            var panel = new Rect(mouse + new Vector2(16f, 16f), size);
            if (panel.xMax > bounds.xMax) panel.x = mouse.x - 16f - size.x;
            if (panel.yMax > bounds.yMax) panel.y = Mathf.Max(bounds.y, bounds.yMax - size.y);

            EditorGUI.DrawRect(panel, new Color(0.08f, 0.08f, 0.09f, 0.95f));
            MapGraphStyle.DrawOutline(panel, MapGraphStyle.TypeColor(node.Type), 1f);
            for (int i = 0; i < _hoverLines.Count; i++)
            {
                _hoverStyle.fontStyle = i == 0 ? FontStyle.Bold : FontStyle.Normal;
                _hoverStyle.normal.textColor = _hoverLines[i].StartsWith("  • ") ? MapGraphStyle.Issue : MapGraphStyle.NodeText;
                GUI.Label(new Rect(panel.x + 6f, panel.y + 5f + i * HoverLineHeight, panel.width - 12f, HoverLineHeight), _hoverLines[i], _hoverStyle);
            }
        }

        // --- Card ------------------------------------------------------------------

        private void DrawCard(MapNode node, MapGraphEditorSession session)
        {
            Rect rect = _rectOf(node);

            if (node.Id == session.SelectedNodeId)
                MapGraphStyle.DrawOutline(rect, MapGraphStyle.Selection, 3f);
            else if (session.IsOnHighlightedPath(node.Id))
                MapGraphStyle.DrawOutline(rect, MapGraphStyle.PathHighlight, 3f);

            Color fill = MapGraphStyle.NodeColor(node, session.Analysis, session.ViewMode, session.Asset.Content);
            EditorGUI.DrawRect(rect, fill);
            UseTextColorFor(fill);
            if (session.ShowDetails) DrawStrips(rect, node, session);
            MapGraphStyle.DrawOutline(rect, new Color(0f, 0f, 0f, 0.5f), 1f);

            string edited = session.IsEdited(node) ? "*" : "";
            if (session.Zoom < MapGraphStyle.LabelMinZoom)
            {
                GUI.Label(rect, $"#{node.Id}{edited}", _titleStyle);
            }
            else if (session.ShowDetails)
            {
                DrawLines(rect, node, session, $"{node.Type} #{node.Id}{edited}");
            }
            else
            {
                var title  = new Rect(rect.x, rect.y + 4f, rect.width, rect.height * 0.5f);
                var detail = new Rect(rect.x, rect.center.y, rect.width, rect.height * 0.5f - 2f);
                GUI.Label(title, $"{node.Type} #{node.Id}{edited}", _titleStyle);
                GUI.Label(detail, ViewModeDetail(node, session), _detailStyle);
            }

            if (session.Asset.IsLocked(node.Id))
            {
                // With details the top corners hold chips, so the lock moves to the right edge's middle.
                float y = session.ShowDetails ? rect.center.y - 9f : rect.y;
                GUI.Label(new Rect(rect.xMax - 18f, y, 18f, 18f), EditorGUIUtility.IconContent("IN LockButton on"));
            }

            if (session.IsDimmed(node.Id))
                EditorGUI.DrawRect(rect, MapGraphStyle.Dimmed);
        }

        // Whichever of dark or light text contrasts more with the fill: dark on bright fills
        // like TECH's amber or Treasure's gold. Above 0.18 relative luminance, black wins.
        private void UseTextColorFor(Color fill)
        {
            Color linear = fill.linear;
            bool bright = 0.2126f * linear.r + 0.7152f * linear.g + 0.0722f * linear.b > 0.18f;
            _titleStyle.normal.textColor  = bright ? new Color(0.08f, 0.08f, 0.08f) : MapGraphStyle.NodeText;
            _detailStyle.normal.textColor = bright ? new Color(0.08f, 0.08f, 0.08f, 0.85f) : new Color(1f, 1f, 1f, 0.8f);
        }

        // A stripe in the room type's colour down the left edge (when colour shows something
        // else), and the intensity as a bar along the bottom.
        private static void DrawStrips(Rect rect, MapNode node, MapGraphEditorSession session)
        {
            if (session.ViewMode != MapGraphViewMode.Type)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, Mathf.Max(2f, 5f * session.Zoom), rect.height), MapGraphStyle.TypeColor(node.Type));

            float height = BarHeight(session);
            var track = new Rect(rect.x, rect.yMax - height, rect.width, height);
            EditorGUI.DrawRect(track, MapGraphStyle.BarTrack);
            EditorGUI.DrawRect(new Rect(track.x, track.y, track.width * node.Intensity, height), MapGraphStyle.IntensityColor(node.Intensity));
        }

        private static float BarHeight(MapGraphEditorSession session) => Mathf.Max(2f, 4f * session.Zoom);

        // The title, then as many detail lines as the card's height allows at this zoom:
        // room, route, then enemies / links / section.
        private void DrawLines(Rect rect, MapNode node, MapGraphEditorSession session, string title)
        {
            LevelBuildSettings build = session.Objectives.BuildSettings;
            float y = rect.y + 2f;
            GUI.Label(new Rect(rect.x + 4f, y, rect.width - 8f, TitleHeight), title, _titleStyle);
            y += TitleHeight;

            float bottom = rect.yMax - BarHeight(session) - 1f;
            for (int line = 0; line < 3 && y + LineHeight <= bottom; line++, y += LineHeight)
            {
                string text = line switch
                {
                    0 => MapNodeInfo.RoomLine(node, session, build),
                    1 => MapNodeInfo.RouteLine(node, session),
                    _ => MapNodeInfo.StatsLine(node, session, build)
                };
                GUI.Label(new Rect(rect.x + 4f, y, rect.width - 8f, LineHeight), text, _detailStyle);
            }
        }

        // The compact card's second line: whatever the current view mode is about.
        private static string ViewModeDetail(MapNode node, MapGraphEditorSession session)
        {
            MapGraphAnalysis analysis = session.Analysis;
            return session.ViewMode switch
            {
                MapGraphViewMode.Intensity        => $"intensity {node.Intensity:0.00}",
                MapGraphViewMode.Faction          => node.HasFaction
                    ? $"{FactionName(session, node.Faction)} {node.FactionInfluence:0.00}"
                      + (node.HasBreachFaction ? $" + {FactionName(session, node.BreachFaction)}" : "")
                    : "no faction",
                MapGraphViewMode.Section          => node.HasSection
                    ? session.Asset.Content?.SectionName(node.Section) ?? node.Section.ToString()
                    : "no section",
                MapGraphViewMode.Zone             => node.Category != RoomCategory.None ? $"{node.Category.Zone()} · {node.Category}" : "no zone",
                MapGraphViewMode.Category         => node.Category != RoomCategory.None ? node.Category.ToString() : "no category",
                MapGraphViewMode.RequiredOptional => !analysis.IsReachable(node.Id) ? "unreachable"
                    : analysis.IsRequired(node.Id) ? "required" : "optional",
                MapGraphViewMode.Branches         => analysis.BranchOf(node.Id) == MapGraphAnalysis.MainPathBranch
                    ? "main path" : $"branch {analysis.BranchOf(node.Id) + 1}",
                _                                 => $"depth {analysis.Depth(node.Id)}"
            };
        }

        private static string FactionName(MapGraphEditorSession session, int faction) =>
            session.Asset.Content?.FactionName(faction) ?? faction.ToString();

        // --- Edge badges and chips -------------------------------------------------

        // "L1 key" or "L2 T1" (its first terminal) along the bottom of each room holding a
        // part, outlined in the lock colour so they read apart from objective badges.
        private void DrawKeyBadges(MapGraphEditorSession session)
        {
            _keyBadgeOffsets.Clear();
            Color color = MapGraphStyle.ConnectionColor(ConnectionType.Locked);
            _keyBadgeStyle.normal.textColor = color;

            for (int l = 0; l < _locks.Count; l++)
            {
                MapConnection connection = _locks[l];
                for (int i = 0; i < connection.KeyNodeIds.Count; i++)
                {
                    if (!session.Graph.TryGetNode(connection.KeyNodeIds[i], out MapNode node)) continue;

                    string name = MapNodeInfo.LockName(l);
                    var content = new GUIContent(connection.UsesSingleKey ? $"{name} key" : $"{name} T{i + 1}");
                    float width = _keyBadgeStyle.CalcSize(content).x + 6f;

                    Rect rect = _rectOf(node);
                    _keyBadgeOffsets.TryGetValue(node.Id, out float offset);
                    var badge = new Rect(rect.x - 6f + offset, rect.yMax - 7f, width, ChipHeight);
                    _keyBadgeOffsets[node.Id] = offset + width + 2f;

                    MapGraphStyle.DrawOutline(badge, color, 1f);
                    EditorGUI.DrawRect(badge, MapGraphStyle.Background);
                    GUI.Label(badge, content, _keyBadgeStyle);
                }
            }
        }

        // The faction's short name in its own colour on the room's bottom-right corner, in
        // every view mode; a Breach room's second faction sits beside it.
        private void DrawFactionChips(MapGraphEditorSession session)
        {
            MapContentSettings content = session.Asset.Content;
            foreach (MapNode node in session.Graph.Nodes)
            {
                if (!node.HasFaction) continue;

                Rect  rect  = _rectOf(node);
                float right = rect.xMax + 6f;
                if (node.HasBreachFaction)
                    right = DrawFactionChip(content, node.BreachFaction, right, rect.yMax - 7f) - 2f;
                DrawFactionChip(content, node.Faction, right, rect.yMax - 7f);
            }
        }

        // Draws the chip ending at `right`; returns where it starts.
        private float DrawFactionChip(MapContentSettings content, int faction, float right, float y)
        {
            FactionDefinition definition = content != null ? content.GetFaction(faction) : null;
            string name  = definition != null ? definition.DisplayName : $"F{faction}";
            if (name.Length > 4) name = name.Substring(0, 4);
            return DrawChip(name.ToUpperInvariant(), MapGraphStyle.FactionColor(content, faction), right, y);
        }

        // "T2" on the room's top-right corner in the tier's colour, with "!3" beside it when
        // validation issues name the room or one of its links.
        private void DrawTierAndIssueChips(MapGraphEditorSession session)
        {
            if (!session.ShowDetails) return;

            foreach (MapNode node in session.Graph.Nodes)
            {
                Rect  rect  = _rectOf(node);
                float right = DrawChip($"T{node.EffectiveTier}", MapGraphStyle.TierColor(node.EffectiveTier), rect.xMax + 6f, rect.y - 9f) - 2f;

                int issues = session.IssuesAbout(node.Id).Count;
                if (issues > 0) DrawChip($"!{issues}", MapGraphStyle.Issue, right, rect.y - 9f);
            }
        }

        // Draws a filled chip ending at `right`; returns where it starts.
        private float DrawChip(string text, Color color, float right, float y)
        {
            var label = new GUIContent(text);
            float width = _chipStyle.CalcSize(label).x + 6f;
            var chip = new Rect(right - width, y, width, ChipHeight);

            EditorGUI.DrawRect(chip, color);
            _chipStyle.normal.textColor = color.grayscale > 0.5f ? Color.black : Color.white;
            GUI.Label(chip, label, _chipStyle);
            return chip.x;
        }

        // "A2" on the corner of the room holding objective A's second step.
        private void DrawObjectiveBadges(MapGraphEditorSession session)
        {
            for (int i = 0; i < _plan.Count; i++)
            {
                Color color = MapObjectivePreview.ColorOf(_plan, i);
                IReadOnlyList<int> steps = _plan[i].NodeIds;
                for (int s = 0; s < steps.Count; s++)
                {
                    if (!session.Graph.TryGetNode(steps[s], out MapNode node)) continue;

                    Rect rect  = _rectOf(node);
                    var  badge = new Rect(rect.x - 6f, rect.y - 9f, 26f, ChipHeight);
                    EditorGUI.DrawRect(badge, color);
                    GUI.Label(badge, $"{MapObjectivePreview.Letter(i)}{s + 1}", _badgeStyle);
                }
            }

            DrawBossRequirement(session);
        }

        // The Boss doors stay shut until every main objective is done: "needs A B".
        private void DrawBossRequirement(MapGraphEditorSession session)
        {
            MapNode boss = session.Graph.FindFirst(MapNodeType.Boss);
            if (boss == null) return;

            string letters = "";
            for (int i = 0; i < _plan.Count; i++)
                if (_plan[i].IsMain) letters += " " + MapObjectivePreview.Letter(i);
            if (letters.Length == 0) return;

            var content = new GUIContent("needs" + letters);
            Rect rect  = _rectOf(boss);
            var  badge = new Rect(rect.x - 6f, rect.y - 9f, _badgeStyle.CalcSize(content).x + 6f, ChipHeight);
            EditorGUI.DrawRect(badge, MapObjectivePreview.MainColor);
            GUI.Label(badge, content, _badgeStyle);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping  = TextClipping.Clip,
                normal    = { textColor = MapGraphStyle.NodeText }
            };
            _detailStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping  = TextClipping.Clip,
                normal    = { textColor = new Color(1f, 1f, 1f, 0.8f) }
            };
            _badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.black }
            };
            _keyBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            _chipStyle     = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            _hoverStyle    = new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip };
        }
    }
}
