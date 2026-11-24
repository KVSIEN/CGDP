using System.Collections.Generic;
using CGD.Level;
using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The Room Lab's right panel: what the room turned out to be (place, size, doorways,
    // how much floor its furniture took, the longest open lane), its issues — click one to
    // outline the tiles it is about — and each furnishing with how many went down.
    public class RoomLabReportPanel
    {
        private Vector2 _scroll;
        private int _selectedIssue = -1;
        private RoomSample _sample;

        // The tiles of the issue picked, for the plan to outline.
        public RectInt? Highlight =>
            _sample != null && _selectedIssue >= 0 && _selectedIssue < _sample.Issues.Count ? _sample.Issues[_selectedIssue].Area : null;

        public void Draw(Rect rect, RoomSample sample)
        {
            if (sample != _sample)
            {
                _sample = sample;
                _selectedIssue = -1;
            }

            GUILayout.BeginArea(rect);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (sample == null || !sample.IsValid)
            {
                EditorGUILayout.LabelField("No room built.", EditorStyles.miniLabel);
            }
            else
            {
                DrawSummary(sample);
                EditorGUILayout.Space(6f);
                DrawPriorities(sample);
                EditorGUILayout.Space(6f);
                DrawIssues(sample);
                EditorGUILayout.Space(6f);
                DrawFurniture(sample);
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static string EnemySummary(RoomEnemyPlan plan)
        {
            if (plan.Spawns.Count == 0) return "none";
            int holding = 0;
            RoomPostKinds kinds = RoomPostKinds.None;
            foreach (PlannedSpawn spawn in plan.Spawns)
            {
                if (spawn.Holds) holding++;
                kinds |= spawn.Kinds;
            }
            return $"{plan.Spawns.Count}: {holding} hold their post, {plan.Spawns.Count - holding} patrol" +
                   (kinds != RoomPostKinds.None ? $" · on {kinds.ToString().ToLowerInvariant()}" : "");
        }

        private static string BreachSummary(RoomSample sample)
        {
            LevelRoom room = sample.Room;
            if (sample.Split == null) return "not split: one zone, both factions dress all of it";
            string holder = room.Faction != null ? room.Faction.DisplayName : "holder";
            string from = room.BreachEntrances.Count > 0 ? $"{room.BreachEntrances.Count} doorway(s) from its rooms" : "no doorway from its rooms: all but the largest zone";
            return $"{holder} zones {ZoneList(sample.Split, false)} · {room.BreachFaction.DisplayName} zones {ZoneList(sample.Split, true)} ({from})";
        }

        private static string ZoneList(BreachSplit split, bool breachSide)
        {
            var names = new System.Text.StringBuilder();
            for (int zone = 0; zone < split.ZoneCount; zone++)
            {
                if (split.IsBreachZone(zone) != breachSide) continue;
                if (names.Length > 0) names.Append(", ");
                names.Append(zone + 1);
            }
            return names.ToString();
        }

        private static void DrawSummary(RoomSample sample)
        {
            LevelRoom room = sample.Room;
            float tile = sample.Settings.TileSize;
            RectInt bounds = room.Footprint.Bounds;

            EditorGUILayout.LabelField(room.DisplayName, EditorStyles.boldLabel);
            EditorGUIUtility.labelWidth = 100f;
            EditorGUILayout.LabelField("Room", $"{room.Node.Type} · tier {room.Node.EffectiveTier}{(room.Faction != null ? $" · {room.Faction.DisplayName}" : "")}"
                                               + (room.BreachFaction != null ? $" + {room.BreachFaction.DisplayName}" : ""));
            if (room.Function != null)
                EditorGUILayout.LabelField("Category", room.Function.Category.ToString());
            if (room.RoomLayout != null)
                EditorGUILayout.LabelField(new GUIContent("Layout", "Which of the place's arrangements this room is: its shapes, size, structure and set pieces"),
                                           new GUIContent(room.RoomLayout.DisplayName));
            EditorGUILayout.LabelField(new GUIContent("Side", "Who the room is for: its section's side where the place can sit on it, else the place's usual side. Scales size, fill and prop counts, and adds the side's dressing"),
                                       new GUIContent(RoomSampleChecks.SideName(room.Side) + (sample.Settings.DesignFor(room.Side) == null && room.Side != ShipSide.None ? " (no side design)" : "")));
            EditorGUILayout.LabelField("Size", $"{bounds.width * tile:0} × {bounds.height * tile:0} m · {room.WallHeight:0.#} m high");
            EditorGUILayout.LabelField("Floor", $"{room.Footprint.Count} tiles{(room.Footprint.CurvedWalls ? " · curved walls" : "")}{(room.Chamfer > 0 ? $" · {room.Chamfer}-tile bevels" : "")}");
            EditorGUILayout.LabelField("Doorways", sample.DoorsWanted >= 0 ? $"{sample.DoorwayCount} of {sample.DoorsWanted}" : sample.DoorwayCount.ToString());
            EditorGUILayout.LabelField("Structure", $"{room.Structure.Pillars.Count} pillars · {room.Structure.Partitions.Count} inner wall edges · {room.Structure.ZoneCount} zone(s)");
            if (room.Structure.Spaces.Count > 0)
                EditorGUILayout.LabelField(new GUIContent("Spaces", "The walled-off places inside the room, each furnished by its own place (a building's classrooms, the streets round it). Each is a zone in the Zones layer"),
                                           new GUIContent(SpaceSummary(room.Structure)));
            if (room.Faction != null)
                EditorGUILayout.LabelField(new GUIContent("Territory", "How strongly the holder's design shows, from the room's map influence (Level Build Settings › Territory Depth)"),
                                           new GUIContent($"{sample.Settings.TerritoryDepth.NameOf(room.DesignStrength)} · design at {room.DesignStrength:0%} (influence {room.Node.FactionInfluence:0.00})"));
            if (room.BreachFaction != null)
                EditorGUILayout.LabelField(new GUIContent("Breach", "Each zone is dressed by one faction: the breaching one takes the zones its doorways open into (else all but the largest). A room in one piece gets the breaching faction's divider first"),
                                           new GUIContent(BreachSummary(sample)));

            EditorGUILayout.LabelField(new GUIContent("Enemies", "Where the room's enemies start (Posts layer): on the tiles their faction favours, holding the post or patrolling their side"),
                                       new GUIContent(EnemySummary(sample.Enemies)));

            CountFurniture(sample, out int pieces, out int floor);
            EditorGUILayout.LabelField("Furniture", $"{pieces} pieces on {floor} tiles ({(float)floor / Mathf.Max(1, room.Footprint.Count):0%} of the floor)");

            EditorGUILayout.LabelField("Seed", sample.SeedText);
            EditorGUIUtility.labelWidth = 0f;
        }

        // "Classroom ×6, Hall, Street": each kind of space and how many there are.
        private static string SpaceSummary(RoomStructure structure)
        {
            var counts = new Dictionary<string, int>();
            foreach (StructureSpace space in structure.Spaces)
            {
                counts.TryGetValue(space.Name, out int count);
                counts[space.Name] = count + 1;
            }

            var parts = new List<string>();
            foreach (KeyValuePair<string, int> entry in counts)
                parts.Add(entry.Value > 1 ? $"{entry.Key} ×{entry.Value}" : entry.Key);
            return string.Join(", ", parts);
        }

        private static string Moved(int moves) => moves > 0 ? $", with {moves} {(moves == 1 ? "prop" : "props")} moved to fit" : "";

        // What the holding faction asks of its fight rooms, next to what this room offers.
        private static void DrawPriorities(RoomSample sample)
        {
            string title = sample.Design != null ? $"Priorities ({(sample.Room.Faction != null ? sample.Room.Faction.DisplayName : sample.Design.name)})" : "Priorities";
            EditorGUILayout.LabelField(new GUIContent(title, "Wants: the faction design's Priorities. Has: measured from this room's floor and furniture"),
                                       EditorStyles.miniBoldLabel);
            if (!RoomSampleChecks.IsFightRoom(sample))
                EditorGUILayout.LabelField("Not a fight room: priorities aren't checked.", EditorStyles.wordWrappedMiniLabel);
            else if (sample.Design == null)
                EditorGUILayout.LabelField("No faction design holds this room: pick a faction to check its priorities.", EditorStyles.wordWrappedMiniLabel);
            else if (sample.Furnishing.Tries > 1)
                EditorGUILayout.LabelField(new GUIContent($"Furnishing try {sample.Furnishing.Attempt + 1} of {sample.Furnishing.Tries} kept, as the closest fit{Moved(sample.Furnishing.Moves)}",
                                                          "The structure and then the furnishing are each built from several seeds (Level Build Settings › Priority Tries), keeping the one closest to these priorities. A furnishing that misses has its loose cover moved about to fit better (Refine Steps)"),
                                           EditorStyles.wordWrappedMiniLabel);

            foreach (RoomTrait trait in RoomSampleChecks.Traits)
            {
                TraitPriority got = sample.Traits.LevelOf(trait);
                string wants = sample.Design != null ? sample.Design.Priorities.Of(trait).ToString() : "—";
                string detail = RoomSampleChecks.Describe(trait, sample);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(new GUIContent(RoomSampleChecks.Name(trait), detail), EditorStyles.miniLabel, GUILayout.Width(90f));
                    GUILayout.Label(new GUIContent($"wants {wants}", detail), EditorStyles.miniLabel, GUILayout.Width(82f));
                    Color previous = GUI.contentColor;
                    if (sample.Design != null) GUI.contentColor = PriorityColor(Mathf.Abs((int)got - (int)sample.Design.Priorities.Of(trait)));
                    GUILayout.Label(new GUIContent($"has {got}", detail), EditorStyles.miniLabel);
                    GUI.contentColor = previous;
                }
                EditorGUILayout.LabelField(detail, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static Color PriorityColor(int off) => off switch
        {
            0 => new Color(0.55f, 0.95f, 0.55f),
            1 => new Color(1f, 0.85f, 0.4f),
            _ => new Color(1f, 0.5f, 0.4f),
        };

        // Pieces counted one per prop (a pile of crates is several); floor once per tile.
        private static void CountFurniture(RoomSample sample, out int pieces, out int floor)
        {
            var tiles = new HashSet<Vector2Int>();
            pieces = 0;
            foreach (PlannedProp prop in sample.Furnishing.Props)
            {
                pieces++;
                if (prop.Decor) continue;
                foreach (Vector2Int tile in prop.Tiles.allPositionsWithin) tiles.Add(tile);
            }
            floor = tiles.Count;
        }

        private void DrawIssues(RoomSample sample)
        {
            EditorGUILayout.LabelField($"Checks ({sample.Issues.Count})", EditorStyles.miniBoldLabel);
            if (sample.Issues.Count == 0)
            {
                EditorGUILayout.LabelField("No issues: every doorway fits, all floor is reachable, nothing came up short.", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            for (int i = 0; i < sample.Issues.Count; i++)
            {
                RoomIssue issue = sample.Issues[i];
                MessageType type = issue.Level switch
                {
                    RoomIssue.Severity.Error   => MessageType.Error,
                    RoomIssue.Severity.Warning => MessageType.Warning,
                    _                          => MessageType.Info,
                };
                EditorGUILayout.HelpBox(issue.Text + (issue.Area.HasValue ? (i == _selectedIssue ? "  (outlined)" : "  — click to show") : ""), type);
                Rect box = GUILayoutUtility.GetLastRect();
                if (issue.Area.HasValue && Event.current.type == EventType.MouseDown && box.Contains(Event.current.mousePosition))
                {
                    _selectedIssue = i == _selectedIssue ? -1 : i;
                    Event.current.Use();
                }
            }
        }

        private static void DrawFurniture(RoomSample sample)
        {
            EditorGUILayout.LabelField("Furnishing, in placement order", EditorStyles.miniBoldLabel);
            if (sample.Furnishing.Outcomes.Count == 0)
            {
                EditorGUILayout.LabelField(sample.Room.Landmark != null ? "A landmark keeps its own interior." : "Nothing to place: no function, dressing or faction props.",
                                           EditorStyles.wordWrappedMiniLabel);
                return;
            }

            foreach (PropPlacementOutcome outcome in sample.Furnishing.Outcomes)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string flags = (outcome.Placement.Required ? " · required" : "") + (outcome.Placement.Decor ? " · decor" : "");
                    EditorGUILayout.LabelField(new GUIContent(outcome.Name + flags, outcome.Explanation), EditorStyles.miniLabel);
                    var counts = new GUIContent($"{outcome.Placed}/{outcome.Wanted}", outcome.Explanation);
                    Color previous = GUI.contentColor;
                    if (outcome.IsShort) GUI.contentColor = outcome.Placed == 0 ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 0.85f, 0.4f);
                    GUILayout.Label(counts, EditorStyles.miniLabel, GUILayout.Width(40f));
                    GUI.contentColor = previous;
                }
            }
        }
    }
}
