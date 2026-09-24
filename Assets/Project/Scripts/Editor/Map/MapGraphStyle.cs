using System.Collections.Generic;
using CGD.Map;
using UnityEngine;

namespace CGD.Editor
{
    // Colours and legends for the Map Graph window, per view mode.
    public static class MapGraphStyle
    {
        public static readonly Vector2 NodeSize = new(130f, 46f);

        public static readonly Color Background   = new(0.16f, 0.16f, 0.17f);
        public static readonly Color GridMinor    = new(1f, 1f, 1f, 0.03f);
        public static readonly Color GridMajor    = new(1f, 1f, 1f, 0.07f);
        public static readonly Color Selection    = new(1f, 0.85f, 0.2f);
        public static readonly Color PathHighlight = new(0.3f, 0.9f, 1f);
        public static readonly Color Dimmed       = new(0f, 0f, 0f, 0.6f);
        public static readonly Color NodeText     = Color.white;

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

        public static Color ConnectionColor(ConnectionType type) => type switch
        {
            ConnectionType.Shortcut => new Color(0.3f, 0.8f, 0.85f),
            ConnectionType.Secret   => new Color(0.7f, 0.4f, 0.9f),
            ConnectionType.Locked   => new Color(0.95f, 0.75f, 0.2f),
            _                       => new Color(0.7f, 0.7f, 0.7f)
        };

        public static Color NodeColor(MapNode node, MapGraphAnalysis analysis, MapGraphViewMode mode) => mode switch
        {
            MapGraphViewMode.Intensity        => Color.Lerp(Calm, Intense, node.Intensity),
            MapGraphViewMode.Faction          => FactionColor(node),
            MapGraphViewMode.RequiredOptional => RequiredColor(node.Id, analysis),
            MapGraphViewMode.Branches         => BranchColor(analysis.BranchOf(node.Id)),
            _                                 => TypeColor(node.Type)
        };

        public static void CollectLegend(MapGraphViewMode mode, MapGenerationSettings settings,
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
                    if (settings != null)
                        for (int i = 0; i < settings.Factions.Count; i++)
                            legend.Add((PaletteColor(i), settings.Factions[i]));
                    break;
                case MapGraphViewMode.RequiredOptional:
                    legend.Add((Required, "Required"));
                    legend.Add((Optional, "Optional"));
                    legend.Add((Unreachable, "Unreachable"));
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

        // Influence fades the faction colour toward neutral, so borders read as contested.
        private static Color FactionColor(MapNode node) =>
            node.HasFaction ? Color.Lerp(NoFaction, PaletteColor(node.Faction), node.FactionInfluence) : NoFaction;

        private static Color RequiredColor(int id, MapGraphAnalysis analysis)
        {
            if (!analysis.IsReachable(id)) return Unreachable;
            return analysis.IsRequired(id) ? Required : Optional;
        }

        private static Color BranchColor(int branch) =>
            branch == MapGraphAnalysis.MainPathBranch ? MainPath : PaletteColor(branch);

        private static Color PaletteColor(int index) => Palette[index % Palette.Length];
    }
}
