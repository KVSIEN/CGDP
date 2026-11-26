using System;
using System.Collections.Generic;
using CGD.Factions;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The room generator's Inspector: every part of the room as a dropdown or a field, each
    // starting on Any (rolled by the seed), so pressing Play with nothing set builds a random
    // room and pinning a few builds the room wanted. The places listed are those of the
    // LevelBuilder on the same object. While playing it says what the room came out as.
    [CustomEditor(typeof(RoomGenerator))]
    public class RoomGeneratorEditor : UnityEditor.Editor
    {
        private const string AnyLabel = "Any";
        private static readonly string[] TierLabels = { AnyLabel, "1", "2", "3" };

        private readonly List<RoomFunction> _places = new();
        private readonly List<RoomLayout> _layouts = new();

        private SerializedProperty _playAs, _content, _player, _spectator;
        private SerializedProperty _seed, _anyType, _type, _category, _place, _layout;
        private SerializedProperty _faction, _second, _section, _influence, _tier, _intensity;
        private SerializedProperty _lowest, _highest, _north, _east, _south, _west;
        private SerializedProperty _overgrowth, _overgrowthProfile;

        private void OnEnable()
        {
            _playAs    = serializedObject.FindProperty("_playAs");
            _content   = serializedObject.FindProperty("_content");
            _player    = serializedObject.FindProperty("_player");
            _spectator = serializedObject.FindProperty("_spectatorCamera");

            SerializedProperty room = serializedObject.FindProperty("_room");
            _seed      = room.FindPropertyRelative(nameof(RoomChoice.SeedText));
            _anyType   = room.FindPropertyRelative(nameof(RoomChoice.AnyType));
            _type      = room.FindPropertyRelative(nameof(RoomChoice.Type));
            _category  = room.FindPropertyRelative(nameof(RoomChoice.Category));
            _place     = room.FindPropertyRelative(nameof(RoomChoice.Place));
            _layout    = room.FindPropertyRelative(nameof(RoomChoice.Layout));
            _faction   = room.FindPropertyRelative(nameof(RoomChoice.Faction));
            _second    = room.FindPropertyRelative(nameof(RoomChoice.SecondFaction));
            _section   = room.FindPropertyRelative(nameof(RoomChoice.Section));
            _influence = room.FindPropertyRelative(nameof(RoomChoice.Influence));
            _tier      = room.FindPropertyRelative(nameof(RoomChoice.Tier));
            _intensity = room.FindPropertyRelative(nameof(RoomChoice.Intensity));
            _lowest    = room.FindPropertyRelative(nameof(RoomChoice.LowestLevel));
            _highest   = room.FindPropertyRelative(nameof(RoomChoice.HighestLevel));
            _north     = room.FindPropertyRelative(nameof(RoomChoice.DoorNorth));
            _east      = room.FindPropertyRelative(nameof(RoomChoice.DoorEast));
            _south     = room.FindPropertyRelative(nameof(RoomChoice.DoorSouth));
            _west      = room.FindPropertyRelative(nameof(RoomChoice.DoorWest));
            _overgrowth        = room.FindPropertyRelative(nameof(RoomChoice.Overgrowth));
            _overgrowthProfile = room.FindPropertyRelative(nameof(RoomChoice.OvergrowthProfile));
        }

        public override void OnInspectorGUI()
        {
            var generator = (RoomGenerator)target;
            LevelBuildSettings settings = generator.TryGetComponent(out LevelBuilder builder) ? builder.Settings : null;
            var content = _content.objectReferenceValue as MapContentSettings;

            serializedObject.Update();
            if (Application.isPlaying) DrawBuilt(generator, content);

            EditorGUILayout.PropertyField(_playAs, new GUIContent("Play as", "Player: stand in the room. Spectator: the Player is switched off and the spectator camera looks round the room, as in the AI Simulator"));
            DrawSeed();

            Section("Room");
            DrawCategory();
            DrawPlace(settings);
            DrawLayout(settings);
            DrawType();

            Section("Faction");
            DrawFactions(content);
            EditorGUILayout.Slider(_influence, 0f, 1f, new GUIContent("Faction hold", "How firmly the faction holds the room: 1 its full design, toward 0.35 its frontier, where more of the plain ship shows"));
            _section.intValue = Popup("Ship section", "Which side of the ship the room is on: first class, below decks…", _section.intValue,
                                      SectionNames(content), RoomChoice.Any, RoomChoice.None);

            Section("Difficulty");
            _tier.intValue = EditorGUILayout.Popup(new GUIContent("Tier", "1 ordinary – 3 toughest"), Mathf.Clamp(_tier.intValue, 0, 3), TierLabels);
            EditorGUILayout.Slider(_intensity, 0f, 1f, new GUIContent("Intensity", "0 calm – 1 peak: how many enemies"));

            Section("Elevation");
            DrawElevation(settings);

            Section("Ground");
            DrawOvergrowth();

            Section("Doorways");
            DrawDoorways();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button(new GUIContent("Set everything to Any", "Every choice rolled by the seed, elevation as broad as the build allows, and a new seed every play")))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(generator, "Randomize room");
                generator.Choice.RandomizeAll();
                EditorUtility.SetDirty(generator);
                serializedObject.Update();
            }

            Section("Scene");
            EditorGUILayout.PropertyField(_content);
            EditorGUILayout.PropertyField(_player);
            EditorGUILayout.PropertyField(_spectator);
            if (settings == null)
                EditorGUILayout.HelpBox("Put this on the same object as the LevelBuilder (with its Settings set) to list the places to choose from.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        // --- Rows ------------------------------------------------------------------------

        private void DrawSeed()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_seed, new GUIContent("Seed", "A number or any text: the same seed and choices always build the same room. Empty = a new room every play"));
                if (GUILayout.Button(new GUIContent("Roll", "Type a new random seed in"), EditorStyles.miniButton, GUILayout.Width(40f)))
                    _seed.stringValue = RoomChoice.NewSeedText();
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_seed.stringValue)))
                    if (GUILayout.Button(new GUIContent("Clear", "A new room every play"), EditorStyles.miniButton, GUILayout.Width(44f)))
                        _seed.stringValue = string.Empty;
            }
        }

        private void DrawCategory()
        {
            string[] labels = Enum.GetNames(typeof(RoomCategory));
            labels[0] = AnyLabel;
            int chosen = EditorGUILayout.Popup(new GUIContent("Room type", "The kind of ship space: commercial, recreation, medical…"), _category.enumValueIndex, labels);
            if (chosen == _category.enumValueIndex) return;
            _category.enumValueIndex = chosen;
            _place.stringValue = _layout.stringValue = string.Empty;
        }

        // Only the places of the chosen room type are listed.
        private void DrawPlace(LevelBuildSettings settings)
        {
            var category = (RoomCategory)_category.enumValueIndex;
            _places.Clear();
            if (settings != null)
                foreach (RoomFunction function in settings.Functions)
                    if (function != null && (category == RoomCategory.None || function.Category == category))
                        _places.Add(function);
            _places.Sort((a, b) => string.Compare(NameOf(a), NameOf(b), StringComparison.OrdinalIgnoreCase));

            var labels = new List<string> { AnyLabel };
            foreach (RoomFunction place in _places)
                labels.Add(category == RoomCategory.None ? $"{NameOf(place)}  ({place.Category})" : NameOf(place));

            int selected = _places.FindIndex(p => p.name == _place.stringValue) + 1;
            int chosen = EditorGUILayout.Popup(new GUIContent("Place", "Which place of that type: a bar, a pool, the bridge… Any = as a level would pick"), selected, labels.ToArray());
            if (chosen == selected) return;
            _place.stringValue = chosen == 0 ? string.Empty : _places[chosen - 1].name;
            _layout.stringValue = string.Empty;
        }

        // Only for a place with layouts.
        private void DrawLayout(LevelBuildSettings settings)
        {
            RoomFunction place = FindPlace(settings);
            _layouts.Clear();
            if (place != null)
                foreach (RoomLayout layout in place.Layouts)
                    if (layout != null) _layouts.Add(layout);
            if (_layouts.Count == 0) return;

            var labels = new List<string> { AnyLabel };
            foreach (RoomLayout layout in _layouts)
                labels.Add(string.IsNullOrEmpty(layout.DisplayName) ? layout.name : layout.DisplayName);

            int selected = _layouts.FindIndex(l => l.name == _layout.stringValue) + 1;
            int chosen = EditorGUILayout.Popup(new GUIContent("Layout", "How the place is laid out and furnished"), selected, labels.ToArray());
            _layout.stringValue = chosen == 0 ? string.Empty : _layouts[chosen - 1].name;
        }

        private void DrawType()
        {
            var labels = new List<string> { AnyLabel };
            foreach (MapNodeType type in RoomChoice.ChoosableTypes) labels.Add(ObjectNames.NicifyVariableName(type.ToString()));

            int selected = _anyType.boolValue ? 0 : Array.IndexOf(RoomChoice.ChoosableTypes, (MapNodeType)_type.intValue) + 1;
            int chosen = EditorGUILayout.Popup(new GUIContent("Encounter", "What happens in the room: a fight, a stealth room, a breach, a shop…"), Mathf.Max(0, selected), labels.ToArray());
            _anyType.boolValue = chosen == 0;
            if (chosen > 0) _type.intValue = (int)RoomChoice.ChoosableTypes[chosen - 1];
        }

        private void DrawFactions(MapContentSettings content)
        {
            var names = new List<string> { AnyLabel, "Mixed", "None (plain ship)" };
            names.AddRange(FactionNames(content));
            _faction.intValue = Popup("Faction", "Who holds the room. Any: one faction, rolled. Mixed: two factions share it, each holding a side. None: no faction, the plain ship",
                                      _faction.intValue, names.ToArray(), RoomChoice.Any, RoomChoice.Mixed, RoomChoice.None);

            bool mixed = _faction.intValue == RoomChoice.Mixed;
            bool twoHeld = !_anyType.boolValue && ((MapNodeType)_type.intValue).HasSecondFaction();
            if (!mixed && !twoHeld) return;

            var seconds = new List<string> { AnyLabel };
            seconds.AddRange(FactionNames(content));
            string label = mixed ? "Mixed with" : "Breached by";
            _second.intValue = Popup(label, "The faction sharing the room; it comes in through the first doorway. Any = rolled, never the holder",
                                     _second.intValue, seconds.ToArray(), RoomChoice.Any);
        }

        private void DrawElevation(LevelBuildSettings settings)
        {
            int lowest = settings != null ? settings.LowestLevel : RoomElevation.Lowest;
            int highest = settings != null ? settings.HighestLevel : RoomElevation.Highest;
            _lowest.intValue = EditorGUILayout.IntSlider(new GUIContent("Lowest level", "How far the floor may sink below the doorways' level (0): sunken floors, craters, lower terraces"),
                                                         Mathf.Clamp(_lowest.intValue, lowest, 0), lowest, 0);
            _highest.intValue = EditorGUILayout.IntSlider(new GUIContent("Highest level", "How far the floor may rise above the doorways' level (0): platforms and mounds at 1, second tiers at 2. Footbridges stand over the floor whatever this says"),
                                                          Mathf.Clamp(_highest.intValue, 0, highest), 0, highest);
            if (_lowest.intValue == 0 && _highest.intValue == 0)
                EditorGUILayout.LabelField(" ", "Flat: the whole floor at the doorways' level", EditorStyles.miniLabel);
            else
                EditorGUILayout.LabelField(" ", "Used by places whose layouts raise or sink floor", EditorStyles.miniLabel);
        }

        private void DrawOvergrowth()
        {
            EditorGUILayout.PropertyField(_overgrowth, new GUIContent("Overgrowth",
                "Faction: what the room's factions grow (Faction Design › Overgrowth), none for most. None: bare floor. Profile: the profile below, all over the room, whoever holds it"));
            if ((RoomOvergrowth)_overgrowth.enumValueIndex != RoomOvergrowth.Profile) return;

            EditorGUILayout.PropertyField(_overgrowthProfile, new GUIContent("Profile", "An Overgrowth Profile asset (Create › CGD › Level › Overgrowth Profile)"));
            if (_overgrowthProfile.objectReferenceValue == null)
                EditorGUILayout.HelpBox("With no profile the floor stays bare.", MessageType.Info);
        }

        private void DrawDoorways()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Door(_north, "North");
                Door(_east, "East");
                Door(_south, "South");
                Door(_west, "West");
            }
            EditorGUILayout.HelpBox("Each doorway leads to a room of its own, rolled by the seed. The first is the way in; none of them has a doorway but this one.", MessageType.None);
            if (!_north.boolValue && !_east.boolValue && !_south.boolValue && !_west.boolValue)
                EditorGUILayout.HelpBox("With no doorway the room has no way in: it is built with its walls closed.", MessageType.Warning);
        }

        private static void Door(SerializedProperty door, string label) =>
            door.boolValue = GUILayout.Toggle(door.boolValue, label, EditorStyles.miniButton);

        // What the room rolled to, while playing: the choices left to Any, filled in.
        private static void DrawBuilt(RoomGenerator generator, MapContentSettings content)
        {
            if (GUILayout.Button(new GUIContent("New room (F5)", "Builds the scene again with a new seed, keeping every pinned choice")))
            {
                generator.Regenerate();
                return;
            }

            RoomRecipe room = generator.Recipe;
            if (room == null) return;

            string place = room.Place != null ? NameOf(room.Place) : room.Category != RoomCategory.None ? $"any {room.Category} place" : "any place";
            string second = room.BreachFaction >= 0 ? $" and {FactionName(content, room.BreachFaction)}" : string.Empty;
            EditorGUILayout.HelpBox(
                $"Built: {ObjectNames.NicifyVariableName(room.Type.ToString())}, {place}\n" +
                $"Held by {FactionName(content, room.Faction)}{second} · {SectionName(content, room.Section)} section\n" +
                $"Tier {room.Tier} · intensity {room.Intensity:0.00} · levels {room.LowestLevel} to {room.HighestLevel} · seed {generator.Seed.Value}\n" +
                "Changes made while playing are lost when play stops; set the room up before pressing Play.",
                MessageType.None);
            DrawSubrooms();
        }

        // The named places inside the built room (a kitchen's cooking area, prep area and walk-in fridge) and
        // the floor levels it climbs through; the same names float over them in the Scene view.
        private static void DrawSubrooms()
        {
            LevelBuilder builder = UnityEngine.Object.FindFirstObjectByType<LevelBuilder>();
            if (builder == null || builder.Layout == null || !builder.Layout.Rooms.TryGetValue(RoomRecipeGraph.RoomId, out LevelRoom built) || built.Structure == null) return;

            var lines = new System.Text.StringBuilder();
            foreach (StructureSpace space in built.Structure.Spaces)
            {
                if (!space.HasArea || string.IsNullOrEmpty(space.Name)) continue;
                lines.Append("• ").Append(space.Name).Append(" · ").Append(space.Area.width).Append(" × ").Append(space.Area.height).Append(" tiles");
                if (space.Function != null) lines.Append(" · ").Append(space.Function.DisplayName);
                lines.Append('\n');
            }
            if (lines.Length == 0) lines.Append("No subrooms. ");
            int low = 0, high = 0;
            foreach (Vector2Int tile in built.Footprint.Tiles)
            {
                low = Mathf.Min(low, built.Structure.LevelOf(tile));
                high = Mathf.Max(high, built.Structure.LevelOf(tile));
            }
            lines.Append(low == 0 && high == 0 ? "Flat floor" : $"Floor levels {low} to {high}");
            EditorGUILayout.HelpBox("Subrooms\n" + lines, MessageType.None);
        }

        // --- Helpers ---------------------------------------------------------------------

        private static void Section(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        // A popup whose first options stand for the given special values, then one per index from 0.
        private static int Popup(string label, string tooltip, int value, string[] options, params int[] specials)
        {
            int selected = Array.IndexOf(specials, value);
            if (selected < 0) selected = value >= 0 && value + specials.Length < options.Length ? value + specials.Length : 0;
            int chosen = EditorGUILayout.Popup(new GUIContent(label, tooltip), selected, options);
            return chosen < specials.Length ? specials[chosen] : chosen - specials.Length;
        }

        private RoomFunction FindPlace(LevelBuildSettings settings)
        {
            if (settings == null || string.IsNullOrEmpty(_place.stringValue)) return null;
            foreach (RoomFunction function in settings.Functions)
                if (function != null && function.name == _place.stringValue) return function;
            return null;
        }

        private static string NameOf(RoomFunction function) =>
            string.IsNullOrEmpty(function.DisplayName) ? function.name : function.DisplayName;

        private static List<string> FactionNames(MapContentSettings content)
        {
            var names = new List<string>();
            if (content != null)
                for (int i = 0; i < content.Factions.Count; i++) names.Add(FactionName(content, i));
            return names;
        }

        private static string[] SectionNames(MapContentSettings content)
        {
            var names = new List<string> { AnyLabel, "None" };
            if (content != null)
                for (int i = 0; i < content.Sections.Count; i++) names.Add(SectionName(content, i));
            return names.ToArray();
        }

        private static string FactionName(MapContentSettings content, int index)
        {
            if (content == null || index < 0 || index >= content.Factions.Count) return "no faction";
            FactionDefinition faction = content.Factions[index];
            return faction != null ? faction.DisplayName : $"Faction {index}";
        }

        private static string SectionName(MapContentSettings content, int index)
        {
            if (content == null || index < 0 || index >= content.Sections.Count) return "no";
            MapSectionDefinition section = content.Sections[index];
            return section != null ? section.DisplayName : $"Section {index}";
        }
    }
}
