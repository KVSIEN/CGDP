using System.Collections.Generic;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The Room Lab's left panel: what room to build. A made-up room is set field by field —
    // anything left on "pick" is chosen by the generator as in a level — and a map room is
    // picked from a saved graph. Returns true when the recipe changed.
    public class RoomLabRecipePanel
    {
        private static readonly string[] Sources = { "Made up", "From a map" };

        private readonly List<RoomFunction> _places = new();
        private readonly List<string> _labels = new();
        private Vector2 _scroll;

        public bool Draw(Rect rect, ref RoomLabRecipe recipe)
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginArea(rect);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUIUtility.labelWidth = 90f;

            recipe.Source = (RoomLabRecipe.SourceKind)GUILayout.Toolbar((int)recipe.Source, Sources);
            EditorGUILayout.Space(4f);
            recipe.BuildSettings = (LevelBuildSettings)EditorGUILayout.ObjectField(
                new GUIContent("Build with", "The Level Build Settings a level would use: room size, shapes, functions, faction designs"),
                recipe.BuildSettings, typeof(LevelBuildSettings), false);
            EditorGUILayout.Space(4f);

            if (recipe.Source == RoomLabRecipe.SourceKind.MapRoom) DrawMapRoom(ref recipe);
            else DrawRecipe(recipe);

            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            return EditorGUI.EndChangeCheck();
        }

        // As the room generator's Inspector: within the build settings' levels.
        private static void DrawLevels(RoomLabRecipe recipe)
        {
            LevelBuildSettings settings = recipe.BuildSettings;
            int lowest = settings != null ? settings.LowestLevel : RoomElevation.Lowest;
            int highest = settings != null ? settings.HighestLevel : RoomElevation.Highest;
            recipe.LowestLevel = EditorGUILayout.IntSlider(new GUIContent("Lowest level", "How far the floor may sink below the doorways' level (0): pits, craters, lower terraces"),
                                                           Mathf.Clamp(recipe.LowestLevel, lowest, 0), lowest, 0);
            recipe.HighestLevel = EditorGUILayout.IntSlider(new GUIContent("Highest level", "How far the floor may rise above the doorways' level (0): platforms, mezzanines, mounds"),
                                                            Mathf.Clamp(recipe.HighestLevel, 0, highest), 0, highest);
        }

        private void DrawRecipe(RoomLabRecipe recipe)
        {
            EditorGUILayout.LabelField("Room", EditorStyles.boldLabel);
            recipe.Type = (MapNodeType)EditorGUILayout.EnumPopup(
                new GUIContent("Type", "What the player meets here: decides cover, clutter, enemies and offerings"), recipe.Type);
            recipe.Category = (RoomCategory)EditorGUILayout.EnumPopup(
                new GUIContent("Category", "The kind of ship space. With no place pinned, one of this category's places is picked"), recipe.Category);
            DrawPlace(recipe);
            DrawLayout(recipe);
            recipe.Tier = EditorGUILayout.IntSlider(new GUIContent("Tier", "1 ordinary – 3 toughest"), recipe.Tier, 1, 3);
            recipe.Intensity = EditorGUILayout.Slider(new GUIContent("Intensity", "0 calm – 1 peak: how many enemies"), recipe.Intensity, 0f, 1f);
            DrawLevels(recipe);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Faction", EditorStyles.boldLabel);
            recipe.Content = (MapContentSettings)EditorGUILayout.ObjectField(
                new GUIContent("Content", "The map content whose faction list the faction below comes from"),
                recipe.Content, typeof(MapContentSettings), false);
            recipe.Faction = FactionPopup("Holder", recipe.Content, recipe.Faction);
            recipe.Influence = EditorGUILayout.Slider(new GUIContent("Influence", "How firmly the holder holds the room on the map: 1 at its territory's origin, 0.35 on its frontier. Lower shows less of its design"),
                                                      recipe.Influence, 0f, 1f);
            if (recipe.Type.HasSecondFaction())
                recipe.BreachFaction = FactionPopup("Breaching", recipe.Content, recipe.BreachFaction);
            recipe.Section = SectionPopup(recipe.Content, recipe.Section);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(new GUIContent("Doorways", "Which walls get a corridor. The floor plan decides where along the wall"), EditorStyles.boldLabel);
            DrawCompass(recipe);
        }

        // A popup of the build settings' places that fit the category, rather than an object
        // field: it shows what the level could actually pick.
        private void DrawPlace(RoomLabRecipe recipe)
        {
            _places.Clear();
            _labels.Clear();
            _places.Add(null);
            _labels.Add("(pick from category)");
            if (recipe.BuildSettings != null)
                foreach (RoomFunction function in recipe.BuildSettings.Functions)
                    if (function != null && (recipe.Category == RoomCategory.None || function.Category == recipe.Category))
                    {
                        _places.Add(function);
                        _labels.Add($"{function.DisplayName}  ({function.Category})");
                    }
            if (recipe.Place != null && !_places.Contains(recipe.Place))
            {
                _places.Add(recipe.Place);
                _labels.Add($"{recipe.Place.DisplayName}  (not in the build settings)");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                int index = EditorGUILayout.Popup(new GUIContent("Place", "Pin the room function to build. Pick = as the level would"),
                                                  _places.IndexOf(recipe.Place), _labels.ToArray());
                recipe.Place = _places[Mathf.Max(0, index)];
                using (new EditorGUI.DisabledScope(recipe.Place == null))
                    if (GUILayout.Button(new GUIContent("Edit", "Select the room function asset; the plan updates as you change it"),
                                         EditorStyles.miniButton, GUILayout.Width(36f)))
                        Selection.activeObject = recipe.Place;
            }
        }

        // Only for a place with layouts: any (as the level picks) or one of them.
        private static void DrawLayout(RoomLabRecipe recipe)
        {
            if (recipe.Place == null || recipe.Place.Layouts.Count == 0)
            {
                recipe.Layout = null;
                return;
            }

            var layouts = new List<RoomLayout> { null };
            var labels  = new List<string> { "(pick by weight)" };
            foreach (RoomLayout layout in recipe.Place.Layouts)
            {
                if (layout == null) continue;
                layouts.Add(layout);
                labels.Add(layout.DisplayName);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                int index = EditorGUILayout.Popup(new GUIContent("Layout", "Pin one of the place's arrangements. Pick = by weight, as the level does; step the seed to see them come round"),
                                                  Mathf.Max(0, layouts.IndexOf(recipe.Layout)), labels.ToArray());
                recipe.Layout = layouts[Mathf.Max(0, index)];
                using (new EditorGUI.DisabledScope(recipe.Layout == null))
                    if (GUILayout.Button(new GUIContent("Edit", "Select the room layout asset; the plan updates as you change it"),
                                         EditorStyles.miniButton, GUILayout.Width(36f)))
                        Selection.activeObject = recipe.Layout;
            }
        }

        private static int FactionPopup(string label, MapContentSettings content, int current)
        {
            if (content == null || content.Factions.Count == 0)
            {
                EditorGUILayout.LabelField(label, "none (no content factions)");
                return MapNode.NoFaction;
            }

            var names = new string[content.Factions.Count + 1];
            names[0] = "None";
            for (int i = 0; i < content.Factions.Count; i++) names[i + 1] = content.FactionName(i);
            int picked = EditorGUILayout.Popup(label, Mathf.Clamp(current + 1, 0, names.Length - 1), names);
            return picked - 1;
        }

        // The section sets the room's side of the ship (guest first class, steerage, crew) where
        // the place can sit on it, so each entry says which side it is.
        private static int SectionPopup(MapContentSettings content, int current)
        {
            var label = new GUIContent("Section", "The ship section the room stands in: its side of the ship, where the place can sit on that side");
            if (content == null || content.Sections.Count == 0)
            {
                EditorGUILayout.LabelField(label, new GUIContent("none (no content sections)"));
                return MapNode.NoSection;
            }

            var names = new string[content.Sections.Count + 1];
            names[0] = "None (the place's usual side)";
            for (int i = 0; i < content.Sections.Count; i++)
            {
                MapSectionDefinition section = content.Sections[i];
                names[i + 1] = section != null ? $"{section.DisplayName} ({RoomSampleChecks.SideName(section.Side)})" : "(missing)";
            }
            int picked = EditorGUILayout.Popup(label, Mathf.Clamp(current + 1, 0, names.Length - 1), names);
            return picked - 1;
        }

        // N / W E / S, laid out as on the plan.
        private static void DrawCompass(RoomLabRecipe recipe)
        {
            const float w = 60f;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(w);
                recipe.DoorNorth = GUILayout.Toggle(recipe.DoorNorth, "North", EditorStyles.miniButton, GUILayout.Width(w));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                recipe.DoorWest = GUILayout.Toggle(recipe.DoorWest, "West", EditorStyles.miniButton, GUILayout.Width(w));
                GUILayout.Space(w);
                recipe.DoorEast = GUILayout.Toggle(recipe.DoorEast, "East", EditorStyles.miniButton, GUILayout.Width(w));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(w);
                recipe.DoorSouth = GUILayout.Toggle(recipe.DoorSouth, "South", EditorStyles.miniButton, GUILayout.Width(w));
            }
            if (recipe.DoorCount == 0)
                EditorGUILayout.HelpBox("A room with no doorway is checked without its walkways and reachability.", MessageType.None);
        }

        private static void DrawMapRoom(ref RoomLabRecipe recipe)
        {
            recipe.Graph = (MapGraphAsset)EditorGUILayout.ObjectField("Map graph", recipe.Graph, typeof(MapGraphAsset), false);
            MapGraph graph = recipe.Graph != null ? recipe.Graph.Graph : null;
            if (graph == null || graph.Nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("Pick a Map Graph asset that has been generated (Window › CGD › Map Graph).", MessageType.Info);
                return;
            }

            var ids = new int[graph.Nodes.Count];
            var names = new string[graph.Nodes.Count];
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                MapNode node = graph.Nodes[i];
                ids[i]   = node.Id;
                names[i] = $"#{node.Id} {node.Type}{(node.Place != null ? $" — {node.Place.DisplayName}" : "")}";
            }
            int current = System.Array.IndexOf(ids, recipe.NodeId);
            recipe.NodeId = ids[Mathf.Max(0, EditorGUILayout.Popup("Room", Mathf.Max(0, current), names))];

            EditorGUILayout.HelpBox($"Built exactly as the level builds it: the whole graph laid out with its seed ({recipe.Graph.SeedText}), its corridors and neighbours included.",
                                    MessageType.None);
            if (GUILayout.Button(new GUIContent("Try variations of this room", "Copy this room into a made-up recipe: same type, place, faction, tier and doorway sides, free to change")))
            {
                if (graph.TryGetNode(recipe.NodeId, out MapNode node))
                {
                    RoomLabRecipe copy = RoomLabRecipe.FromMapNode(recipe.Graph, node, recipe.BuildSettings);
                    copy.Graph  = recipe.Graph;
                    copy.NodeId = recipe.NodeId;
                    recipe = copy;
                    GUI.changed = true;
                }
            }
        }
    }
}
