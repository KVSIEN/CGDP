using System;
using System.Collections.Generic;
using CGD.Core;
using CGD.Level;
using CGD.Map;
using UnityEngine;

namespace CGD.Editor
{
    // The objectives a level built from the open graph would get, for the Map Graph window:
    // the same planner fed the same seed LevelBuilder uses for a graph asset (the asset's
    // seed, when the builder's own seed is 0), so where each step lands can be judged in 2D.
    // Whether a room will have enemies to clear is read from the session's build settings'
    // room rules; a faction with an empty roster isn't checked, so a Clear step may very
    // rarely land differently in the level.
    public class MapObjectivePreview
    {
        private static readonly IReadOnlyList<PlannedObjective> Empty = Array.Empty<PlannedObjective>();

        public static readonly Color MainColor = new(1f, 0.85f, 0.2f);
        private static readonly Color[] SideColors =
        {
            new(0.35f, 0.85f, 0.95f), new(0.75f, 0.55f, 1f), new(0.45f, 0.9f, 0.55f), new(1f, 0.55f, 0.75f),
        };

        private MapObjectiveSettings _settings;
        private bool _searched;

        private List<PlannedObjective> _plan;
        private int  _planVersion;
        private Seed _planSeed;
        private LevelBuildSettings _planBuild;
        private string _bossRequirement;

        private static readonly Dictionary<int, string> StepLabels = new();

        // The objective settings the level would use; the project's first until another is picked.
        public MapObjectiveSettings Settings
        {
            get
            {
                if (_settings == null && !_searched)
                {
                    _searched = true;
                    _settings = MapEditorAssets.FindFirst<MapObjectiveSettings>();
                }
                return _settings;
            }
            set { if (_settings == value) return; _settings = value; _plan = null; }
        }

        // Replanned whenever the graph, its seed or the build settings change.
        public IReadOnlyList<PlannedObjective> PlanFor(MapGraphEditorSession session)
        {
            if (!session.HasGraph || Settings == null) return Empty;
            LevelBuildSettings build = session.BuildSettings;
            if (_plan != null && _planVersion == session.Version && _planSeed == session.Asset.Seed && _planBuild == build) return _plan;

            RandomStream random = session.Asset.Seed.Derive("objectives").Stream();
            var planner = new MapObjectivePlanner(session.Graph, random, node => WillHaveEnemies(node, build));
            _plan        = planner.Plan(_settings.Templates, _settings.MainCount.Evaluate(random), _settings.SideCount.Evaluate(random));
            _planVersion = session.Version;
            _planSeed    = session.Asset.Seed;
            _planBuild   = build;
            _bossRequirement = BuildBossRequirement(_plan);
            return _plan;
        }

        // "needs A B": the main objectives the Boss doors wait on, for the plan PlanFor last
        // returned; null when there are none (or for any other list).
        public string BossRequirement(IReadOnlyList<PlannedObjective> plan) =>
            ReferenceEquals(plan, _plan) ? _bossRequirement : null;

        private static string BuildBossRequirement(IReadOnlyList<PlannedObjective> plan)
        {
            string letters = "";
            for (int i = 0; i < plan.Count; i++)
                if (plan[i].IsMain) letters += " " + Letter(i);
            return letters.Length > 0 ? "needs" + letters : null;
        }

        // Objectives are lettered in plan order (main ones first): A, B, C…
        public static string Letter(int index) => ((char)('A' + index)).ToString();

        // "A2": objective A's second step. Kept, since badges redraw on every repaint.
        public static string StepLabel(int objective, int step)
        {
            int key = objective * 1000 + step;
            if (!StepLabels.TryGetValue(key, out string label))
                StepLabels[key] = label = $"{Letter(objective)}{step + 1}";
            return label;
        }

        public static Color ColorOf(IReadOnlyList<PlannedObjective> plan, int index)
        {
            if (plan[index].IsMain) return MainColor;
            int side = 0;
            for (int i = 0; i < index; i++)
                if (!plan[i].IsMain) side++;
            return SideColors[side % SideColors.Length];
        }

        // Without build settings every room counts as having enemies.
        private static bool WillHaveEnemies(MapNode node, LevelBuildSettings build) =>
            build == null || MapNodeInfo.EnemyCount(node, build) > 0;
    }
}
