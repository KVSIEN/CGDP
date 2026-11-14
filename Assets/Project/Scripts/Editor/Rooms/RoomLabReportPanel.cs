using System.Collections.Generic;
using CGD.Level;
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
                DrawIssues(sample);
                EditorGUILayout.Space(6f);
                DrawFurniture(sample);
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
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
            EditorGUILayout.LabelField("Size", $"{bounds.width * tile:0} × {bounds.height * tile:0} m · {room.WallHeight:0.#} m high");
            EditorGUILayout.LabelField("Floor", $"{room.Footprint.Count} tiles{(room.Footprint.CurvedWalls ? " · curved walls" : "")}{(room.Chamfer > 0 ? $" · {room.Chamfer}-tile bevels" : "")}");
            EditorGUILayout.LabelField("Doorways", sample.DoorsWanted >= 0 ? $"{sample.DoorwayCount} of {sample.DoorsWanted}" : sample.DoorwayCount.ToString());
            EditorGUILayout.LabelField("Structure", $"{room.Structure.Pillars.Count} pillars · {room.Structure.Partitions.Count} inner wall edges · {room.Structure.ZoneCount} zone(s)");

            CountFurniture(sample, out int pieces, out int floor);
            EditorGUILayout.LabelField("Furniture", $"{pieces} pieces on {floor} tiles ({(float)floor / Mathf.Max(1, room.Footprint.Count):0%} of the floor)");

            RoomSampleChecks.LongestLane(sample, out int lane);
            EditorGUILayout.LabelField(new GUIContent("Longest lane", "The longest unbroken row or column of see-through floor (Sightlines layer)"),
                                       new GUIContent($"{lane * tile:0} m"));
            EditorGUILayout.LabelField("Seed", sample.SeedText);
            EditorGUIUtility.labelWidth = 0f;
        }

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
