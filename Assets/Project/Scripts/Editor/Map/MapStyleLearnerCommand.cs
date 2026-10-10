using System.Collections.Generic;
using System.IO;
using CGD.Core;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Assets › CGD › Learn Map Style From Selected Graphs: measures the selected Map Graph
    // assets (MapStyleLearner) and writes a new generation style tuned to match — layout,
    // content and category settings plus the style that ties them together. Everything the
    // examples can't tell (factions, sections, modifiers, intensity, guarantees…) is copied
    // from the first graph's own style, so new maps keep the rest of the game's tuning.
    public static class MapStyleLearnerCommand
    {
        private const string MenuPath = "Assets/CGD/Learn Map Style From Selected Graphs";
        private static readonly Vector2 DefaultSpacing = new(220f, 110f);

        [MenuItem(MenuPath, true)]
        private static bool CanLearn() => SelectedGraphs().Count > 0;

        [MenuItem(MenuPath, false, 200)]
        private static void Learn()
        {
            List<MapGraphAsset> examples = SelectedGraphs();
            MapGenerationSettings baseStyle = examples.Find(g => g.Settings != null && g.Settings.CanGenerate)?.Settings;
            if (baseStyle == null)
            {
                EditorUtility.DisplayDialog("Learn Map Style", "At least one selected graph needs a generation style (with content and a layout) to start from.", "OK");
                return;
            }

            var graphs   = new List<MapGraph>();
            var spacings = new List<Vector2>();
            foreach (MapGraphAsset example in examples)
            {
                graphs.Add(example.Graph);
                spacings.Add(example.Settings != null ? example.Settings.NodeSpacing : DefaultSpacing);
            }

            LearnedMapStyle style = MapStyleLearner.Learn(graphs, spacings, baseStyle.Content);
            if (style.MapCount == 0)
            {
                EditorUtility.DisplayDialog("Learn Map Style", string.Join("\n", style.Notes), "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject("Save Learned Style", $"{examples[0].name}Style",
                "asset", "Name the new style. Its layout, content and category settings are saved beside it.",
                Path.GetDirectoryName(AssetDatabase.GetAssetPath(examples[0])));
            if (string.IsNullOrEmpty(path)) return;

            MapGenerationSettings created = Write(style, baseStyle, path);
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            EditorUtility.DisplayDialog("Learn Map Style", string.Join("\n", style.Notes) +
                "\n\nPick the new style on a Map Graph asset and press New Seed to see maps in this style.", "OK");
        }

        private static List<MapGraphAsset> SelectedGraphs()
        {
            var graphs = new List<MapGraphAsset>();
            foreach (Object selected in Selection.objects)
                if (selected is MapGraphAsset graph) graphs.Add(graph);
            return graphs;
        }

        // --- Writing assets ----------------------------------------------------------

        private static MapGenerationSettings Write(LearnedMapStyle style, MapGenerationSettings baseStyle, string path)
        {
            string folder = Path.GetDirectoryName(path);
            string name   = Path.GetFileNameWithoutExtension(path);
            if (name.EndsWith("Style")) name = name.Substring(0, name.Length - "Style".Length);

            MapContentSettings baseContent = baseStyle.Content;
            MapCategorySettings categories = baseContent.Categories != null
                ? Object.Instantiate(baseContent.Categories)
                : ScriptableObject.CreateInstance<MapCategorySettings>();
            if (style.HasCategories) ApplyCategories(categories, style);
            Create(categories, folder, $"{name}MapCategorySettings");

            MapContentSettings content = Object.Instantiate(baseContent);
            ApplyContent(content, style, categories);
            Create(content, folder, $"{name}MapContentSettings");

            MapLayoutSettings layout = Object.Instantiate(FirstLayout(baseStyle));
            ApplyLayout(layout, style);
            Create(layout, folder, $"{name}MapLayoutSettings");

            MapGenerationSettings generation = Object.Instantiate(baseStyle);
            var serialized = new SerializedObject(generation);
            SerializedProperty layouts = serialized.FindProperty("_layouts");
            layouts.arraySize = 1;
            layouts.GetArrayElementAtIndex(0).FindPropertyRelative("_layout").objectReferenceValue = layout;
            layouts.GetArrayElementAtIndex(0).FindPropertyRelative("_weight").floatValue = 1f;
            serialized.FindProperty("_content").objectReferenceValue = content;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Create(generation, folder, $"{name}MapGenerationSettings");

            AssetDatabase.SaveAssets();
            return generation;
        }

        private static MapLayoutSettings FirstLayout(MapGenerationSettings style)
        {
            foreach (MapLayoutOption option in style.Layouts)
                if (option.Layout != null) return option.Layout;
            return null;
        }

        private static void Create(Object asset, string folder, string name) =>
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset"));

        private static void ApplyLayout(MapLayoutSettings layout, LearnedMapStyle style)
        {
            var so = new SerializedObject(layout);
            SetRange(so.FindProperty("_optionalRooms"), style.OptionalRooms);
            so.FindProperty("_minBossDepth").intValue          = style.MinBossDepth;
            so.FindProperty("_maxSpread").intValue             = style.MaxSpread;
            so.FindProperty("_maxConnectionsPerNode").intValue = style.MaxConnectionsPerNode;
            so.FindProperty("_directDoorChance").floatValue    = style.DirectDoorChance;

            so.FindProperty("_mainPath._direction").intValue = (int)style.Direction;
            SetRange(so.FindProperty("_mainPath._length"), style.PathLength);
            so.FindProperty("_mainPath._winding").floatValue = style.PathWinding;

            if (style.BranchLength.HasValue) SetRange(so.FindProperty("_branches._length"), style.BranchLength.Value);
            SetIfLearned(so.FindProperty("_branches._winding"), style.BranchWinding);
            SetIfLearned(so.FindProperty("_branches._forkChance"), style.ForkChance);
            SetRange(so.FindProperty("_branches._hubBranches"), style.HubBranches);
            // Every extra link was counted as a loop, so branches don't add more of their own.
            so.FindProperty("_branches._rejoinChance").floatValue = 0f;

            SetRange(so.FindProperty("_loops._loopCount"), style.LoopCount);
            SetRange(so.FindProperty("_loops._shortcutCount"), style.ShortcutCount);
            SetIfLearned(so.FindProperty("_loops._oneWayShortcutChance"), style.OneWayShortcutChance);

            so.FindProperty("_gates._lockedChance").floatValue = style.LockedChance;
            so.FindProperty("_gates._secretChance").floatValue = style.SecretChance;
            so.FindProperty("_gates._maxGates").intValue       = style.MaxGates;
            if (style.GateMinDepth >= 0) so.FindProperty("_gates._minDepth").intValue = style.GateMinDepth;
            SetIfLearned(so.FindProperty("_gates._terminalLockChance"), style.TerminalLockChance);
            if (style.TerminalCount.HasValue) SetRange(so.FindProperty("_gates._terminalCount"), style.TerminalCount.Value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Rules for types the examples used are rewritten from them; unused types get a
        // maximum of 0. Fields the examples can't speak to (intensity, wanting space…) keep
        // the base rule's value.
        private static void ApplyContent(MapContentSettings content, LearnedMapStyle style, MapCategorySettings categories)
        {
            var so = new SerializedObject(content);
            SerializedProperty rules = so.FindProperty("_nodeRules");

            foreach (LearnedTypeRule learned in style.TypeRules)
            {
                SerializedProperty rule = FindRule(rules, learned.Type);
                if (rule == null)
                {
                    if (!learned.Seen) continue;
                    rules.arraySize++;
                    rule = rules.GetArrayElementAtIndex(rules.arraySize - 1);
                    rule.FindPropertyRelative("_type").intValue = (int)learned.Type;
                }

                rule.FindPropertyRelative("_min").intValue = learned.Seen ? learned.Min : 0;
                rule.FindPropertyRelative("_max").intValue = learned.Seen ? learned.Max : 0;
                if (!learned.Seen) continue;

                rule.FindPropertyRelative("_weight").floatValue           = learned.Weight;
                rule.FindPropertyRelative("_placement").intValue          = (int)learned.Placement;
                rule.FindPropertyRelative("_minDepth").floatValue         = learned.MinDepth;
                rule.FindPropertyRelative("_maxDepth").floatValue         = learned.MaxDepth;
                rule.FindPropertyRelative("_preferDeadEnds").boolValue    = learned.PreferDeadEnds;
                if (learned.SeenAdjacent) rule.FindPropertyRelative("_allowAdjacentSameType").boolValue = true;

                SerializedProperty spacing = rule.FindPropertyRelative("_minSpacing");
                if (learned.ClosestSpacing > 0 && learned.ClosestSpacing < spacing.intValue) spacing.intValue = learned.ClosestSpacing;
            }

            so.FindProperty("_pacing._maxCombatInARow").intValue = style.MaxCombatInARow;
            so.FindProperty("_categories").objectReferenceValue  = categories;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindRule(SerializedProperty rules, MapNodeType type)
        {
            for (int i = 0; i < rules.arraySize; i++)
            {
                SerializedProperty rule = rules.GetArrayElementAtIndex(i);
                if (rule.FindPropertyRelative("_type").intValue == (int)type) return rule;
            }
            return null;
        }

        // Category weights and zone shares come from the examples; affinities the examples
        // show replace the base pair (or are added), the rest of the base table is kept.
        private static void ApplyCategories(MapCategorySettings categories, LearnedMapStyle style)
        {
            var so = new SerializedObject(categories);

            SerializedProperty list = so.FindProperty("_categories");
            foreach (KeyValuePair<RoomCategory, float> weight in style.CategoryWeights)
                Entry(list, "_category", (int)weight.Key).FindPropertyRelative("_weight").floatValue = weight.Value;

            SerializedProperty zones = so.FindProperty("_zones");
            foreach (KeyValuePair<ShipZone, float> share in style.ZoneShares)
                if (share.Key != ShipZone.None)
                    Entry(zones, "_zone", (int)share.Key).FindPropertyRelative("_share").floatValue = share.Value;

            SerializedProperty affinities = so.FindProperty("_affinities");
            foreach ((RoomCategory a, RoomCategory b, float affinity) in style.Affinities)
                AffinityEntry(affinities, a, b).FindPropertyRelative("_affinity").floatValue = affinity;

            if (style.SameCategoryAffinity >= 0f)
                so.FindProperty("_sameCategoryAffinity").floatValue = style.SameCategoryAffinity;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // The list element whose `key` field is `value`, added when missing.
        private static SerializedProperty Entry(SerializedProperty list, string key, int value)
        {
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue == value)
                    return list.GetArrayElementAtIndex(i);

            list.arraySize++;
            SerializedProperty added = list.GetArrayElementAtIndex(list.arraySize - 1);
            added.FindPropertyRelative(key).intValue = value;
            added.FindPropertyRelative("_preferredSections")?.ClearArray();   // copied from the element before
            return added;
        }

        private static SerializedProperty AffinityEntry(SerializedProperty list, RoomCategory a, RoomCategory b)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty pair = list.GetArrayElementAtIndex(i);
                int x = pair.FindPropertyRelative("_a").intValue, y = pair.FindPropertyRelative("_b").intValue;
                if ((x == (int)a && y == (int)b) || (x == (int)b && y == (int)a)) return pair;
            }

            list.arraySize++;
            SerializedProperty added = list.GetArrayElementAtIndex(list.arraySize - 1);
            added.FindPropertyRelative("_a").intValue = (int)a;
            added.FindPropertyRelative("_b").intValue = (int)b;
            return added;
        }

        private static void SetRange(SerializedProperty range, IntRange value)
        {
            range.FindPropertyRelative("Min").intValue = value.Min;
            range.FindPropertyRelative("Max").intValue = value.Max;
        }

        private static void SetIfLearned(SerializedProperty property, float value)
        {
            if (value >= 0f) property.floatValue = value;
        }
    }
}
