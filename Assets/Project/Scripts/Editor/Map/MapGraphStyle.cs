using System.Collections.Generic;
using CGD.Factions;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Colours, legends and the outline helper for the Map Graph window.
    public static class MapGraphStyle
    {
        public static readonly Vector2 NodeSize = new(160f, 70f);
        // Below this zoom node and link text is left out.
        public const float LabelMinZoom = 0.6f;

        public static readonly Color Background   = new(0.16f, 0.16f, 0.17f);
        public static readonly Color GridMinor    = new(1f, 1f, 1f, 0.03f);
        public static readonly Color GridMajor    = new(1f, 1f, 1f, 0.07f);
        public static readonly Color Selection    = new(1f, 0.85f, 0.2f);
        public static readonly Color PathHighlight = new(0.3f, 0.9f, 1f);
        public static readonly Color Dimmed       = new(0f, 0f, 0f, 0.6f);
        public static readonly Color NodeText     = Color.white;
        public static readonly Color Issue        = new(0.95f, 0.25f, 0.2f);
        public static readonly Color BarTrack     = new(0f, 0f, 0f, 0.35f);

        private static readonly Color Required    = new(0.9f, 0.55f, 0.15f);
        private static readonly Color Optional    = new(0.35f, 0.5f, 0.75f);
        private static readonly Color Unreachable = new(0.55f, 0.15f, 0.15f);
        private static readonly Color MainPath    = new(0.75f, 0.75f, 0.75f);
        private static readonly Color NoFaction   = new(0.35f, 0.35f, 0.35f);
        private static readonly Color Calm        = new(0.2f, 0.6f, 0.35f);
        private static readonly Color Intense     = new(0.85f, 0.2f, 0.2f);

        private static readonly Color[] Palette =
        {
            new(0.85f, 0.35f, 0.35f), new(0.35f, 0.65f, 0.9f), new(0.45f, 0.8f, 0.4f),
            new(0.9f, 0.7f, 0.25f),   new(0.7f, 0.45f, 0.9f),  new(0.3f, 0.8f, 0.75f),
            new(0.9f, 0.5f, 0.75f),   new(0.6f, 0.6f, 0.3f)
        };

        public static Color TypeColor(MapNodeType type) => MapNodeColors.Of(type);

        public static Color IntensityColor(float intensity) => Color.Lerp(Calm, Intense, intensity);

        public static Color TierColor(int tier) => tier switch
        {
            1 => new Color(0.45f, 0.75f, 0.45f),
            2 => new Color(0.95f, 0.7f, 0.25f),
            _ => new Color(0.9f, 0.3f, 0.3f)
        };

        public static Color ConnectionColor(ConnectionType type) => type switch
        {
            ConnectionType.Shortcut => new Color(0.3f, 0.8f, 0.85f),
            ConnectionType.Secret   => new Color(0.7f, 0.4f, 0.9f),
            ConnectionType.Locked   => new Color(0.95f, 0.75f, 0.2f),
            _                       => new Color(0.7f, 0.7f, 0.7f)
        };

        // `content` resolves faction indices to their assets' colours (null = the generic palette).
        public static Color NodeColor(MapNode node, MapGraphAnalysis analysis, MapGraphViewMode mode, MapContentSettings content) => mode switch
        {
            MapGraphViewMode.Intensity        => IntensityColor(node.Intensity),
            MapGraphViewMode.Faction          => InfluencedFactionColor(node, content),
            MapGraphViewMode.RequiredOptional => RequiredColor(node.Id, analysis),
            MapGraphViewMode.Branches         => BranchColor(analysis.BranchOf(node.Id)),
            MapGraphViewMode.Section          => node.HasSection ? PaletteColor(node.Section) : NoFaction,
            MapGraphViewMode.Category         => node.Category != RoomCategory.None ? PaletteColor((int)node.Category - 1) : NoFaction,
            MapGraphViewMode.Zone             => node.Category != RoomCategory.None ? PaletteColor((int)node.Category.Zone() - 1) : NoFaction,
            _                                 => TypeColor(node.Type)
        };

        public static void CollectLegend(MapGraphViewMode mode, MapContentSettings content,
                                         MapGraphAnalysis analysis, List<(Color color, string label)> legend)
        {
            legend.Clear();
            switch (mode)
            {
                case MapGraphViewMode.Type:
                    foreach (MapNodeType type in System.Enum.GetValues(typeof(MapNodeType)))
                        legend.Add((TypeColor(type), type.ToString()));
                    break;
                case MapGraphViewMode.Intensity:
                    legend.Add((Calm, "Calm"));
                    legend.Add((Color.Lerp(Calm, Intense, 0.5f), "Medium"));
                    legend.Add((Intense, "Intense"));
                    break;
                case MapGraphViewMode.Faction:
                    legend.Add((NoFaction, "None"));
                    if (content != null)
                        for (int i = 0; i < content.Factions.Count; i++)
                            legend.Add((FactionColor(content, i), content.FactionName(i)));
                    break;
                case MapGraphViewMode.RequiredOptional:
                    legend.Add((Required, "Required"));
                    legend.Add((Optional, "Optional"));
                    legend.Add((Unreachable, "Unreachable"));
                    break;
                case MapGraphViewMode.Section:
                    legend.Add((NoFaction, "None"));
                    if (content != null)
                        for (int i = 0; i < content.Sections.Count; i++)
                            legend.Add((PaletteColor(i), content.SectionName(i)));
                    break;
                case MapGraphViewMode.Category:
                    legend.Add((NoFaction, "None"));
                    foreach (RoomCategory category in System.Enum.GetValues(typeof(RoomCategory)))
                        if (category != RoomCategory.None)
                            legend.Add((PaletteColor((int)category - 1), category.ToString()));
                    break;
                case MapGraphViewMode.Zone:
                    legend.Add((NoFaction, "None"));
                    foreach (ShipZone zone in System.Enum.GetValues(typeof(ShipZone)))
                        if (zone != ShipZone.None)
                            legend.Add((PaletteColor((int)zone - 1), zone.ToString()));
                    break;
                case MapGraphViewMode.Branches:
                    legend.Add((MainPath, "Main path"));
                    for (int i = 0; i < Mathf.Min(analysis.BranchCount, Palette.Length); i++)
                        legend.Add((PaletteColor(i), $"Branch {i + 1}"));
                    break;
            }

            foreach (ConnectionType type in System.Enum.GetValues(typeof(ConnectionType)))
                legend.Add((ConnectionColor(type), $"{type} link"));
        }

        // A faction's own colour (its FactionDefinition, the reality's signal colour in the GDD),
        // or the generic palette for a faction index without an asset.
        public static Color FactionColor(MapContentSettings content, int faction)
        {
            FactionDefinition definition = content != null ? content.GetFaction(faction) : null;
            return definition != null ? definition.Color : PaletteColor(faction);
        }

        // Influence fades the faction colour toward neutral, so borders read as contested.
        private static Color InfluencedFactionColor(MapNode node, MapContentSettings content) =>
            node.HasFaction ? Color.Lerp(NoFaction, FactionColor(content, node.Faction), node.FactionInfluence) : NoFaction;

        private static Color RequiredColor(int id, MapGraphAnalysis analysis)
        {
            if (!analysis.IsReachable(id)) return Unreachable;
            return analysis.IsRequired(id) ? Required : Optional;
        }

        private static Color BranchColor(int branch) =>
            branch == MapGraphAnalysis.MainPathBranch ? MainPath : PaletteColor(branch);

        public static Color PaletteColor(int index) => Palette[index % Palette.Length];

        public static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y - thickness, rect.width + thickness * 2f, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.yMax, rect.width + thickness * 2f, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax, rect.y, thickness, rect.height), color);
        }
    }
}
