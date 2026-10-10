using System;
using System.Collections.Generic;
using CGD.Core;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The objectives a level built from the open graph would get, for the Map Graph window:
    // the same planner fed the same seed LevelBuilder uses for a graph asset (the asset's
    // seed, when the builder's own seed is 0), so where each step lands can be judged in 2D.
    // Whether a room will have enemies to clear is read from the build settings' room
    // rules; a faction with an empty roster isn't checked, so a Clear step may very rarely
    // land differently in the level.
    public class MapObjectivePreview
    {
        private static readonly IReadOnlyList<PlannedObjective> Empty = Array.Empty<PlannedObjective>();

        public static readonly Color MainColor = new(1f, 0.85f, 0.2f);
        private static readonly Color[] SideColors =
        {
            new(0.35f, 0.85f, 0.95f), new(0.75f, 0.55f, 1f), new(0.45f, 0.9f, 0.55f), new(1f, 0.55f, 0.75f),
        };

        private MapObjectiveSettings _settings;
        private LevelBuildSettings   _buildSettings;
        private bool _searched;

        private List<PlannedObjective> _plan;
        private int _planVersion;
        private int _planSeed;

        public MapObjectiveSettings Settings
        {
            get { FindDefaults(); return _settings; }
            set { if (_settings == value) return; _settings = value; _plan = null; }
        }

        public LevelBuildSettings BuildSettings
        {
            get { FindDefaults(); return _buildSettings; }
            set { if (_buildSettings == value) return; _buildSettings = value; _plan = null; }
        }

        // Replanned whenever the graph or its seed changes.
        public IReadOnlyList<PlannedObjective> PlanFor(MapGraphEditorSession session)
        {
            if (!session.HasGraph || Settings == null) return Empty;
            if (_plan != null && _planVersion == session.Version && _planSeed == session.Asset.Seed) return _plan;

            RandomStream random = Seed.From(session.Asset.Seed).Derive("objectives").Stream();
            var planner = new MapObjectivePlanner(session.Graph, random, WillHaveEnemies);
            _plan        = planner.Plan(_settings.Templates, _settings.MainCount.Evaluate(random), _settings.SideCount.Evaluate(random));
            _planVersion = session.Version;
            _planSeed    = session.Asset.Seed;
            return _plan;
        }

        // Objectives are lettered in plan order (main ones first): A, B, C…
        public static string Letter(int index) => ((char)('A' + index)).ToString();

        public static Color ColorOf(IReadOnlyList<PlannedObjective> plan, int index)
        {
            if (plan[index].IsMain) return MainColor;
            int side = 0;
            for (int i = 0; i < index; i++)
                if (!plan[i].IsMain) side++;
            return SideColors[side % SideColors.Length];
        }

        private bool WillHaveEnemies(MapNode node)
        {
            if (_buildSettings == null) return true;
            RoomContentRule rule = _buildSettings.RuleFor(node.Type);
            return rule != null && rule.EnemyCount.Lerp(node.Intensity) > 0;
        }

        // The project's first of each, until another is picked.
        private void FindDefaults()
        {
            if (_searched) return;
            _searched = true;
            if (_settings == null)      _settings      = FindFirst<MapObjectiveSettings>();
            if (_buildSettings == null) _buildSettings = FindFirst<LevelBuildSettings>();
        }

        private static T FindFirst<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }
    }
}
