using System.Collections.Generic;
using CGD.Level;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Draws a RoomLayoutCheck in an Inspector: a verdict, then a row per piece with how often
    // it went down and how many of what it asked for. A row in red is a piece that sometimes
    // goes missing; hover it for why.
    public static class RoomLayoutCheckView
    {
        private const float FromWidth = 92f;
        private const float FitsWidth = 44f;
        private const float CountWidth = 64f;

        private static GUIStyle _short;

        public static void Draw(RoomLayoutCheck check)
        {
            if (check == null) return;
            EditorGUILayout.HelpBox(check.Summary, check.IsSound ? MessageType.Info : MessageType.Warning);
            if (check.Built == 0) return;

            Row(EditorStyles.miniBoldLabel, new GUIContent("Piece"), "From", "Fits", "Placed / asked");
            foreach (RoomLayoutCheckPiece piece in check.Pieces)
            {
                if (piece.Rooms == 0)
                {
                    Row(EditorStyles.miniLabel, new GUIContent(Label(piece), "Only for rooms bigger than any this check built"), piece.Source, "—", "left out");
                    continue;
                }
                bool sometimesMissing = piece.RoomsWithNone > 0;
                var name = new GUIContent(Label(piece), piece.Shortfall ?? "Placed as asked in every room");
                Row(sometimesMissing ? ShortStyle : EditorStyles.miniLabel, name, piece.Source, $"{piece.FitRate:P0}",
                    $"{piece.Placed / (float)piece.Rooms:0.#} / {piece.Wanted / (float)piece.Rooms:0.#}");
            }
        }

        // One layout's verdict on a line, with a button that selects the layout.
        public static void DrawLine(RoomLayoutCheck check)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string name = check.Layout != null ? check.Layout.DisplayName : check.Place.DisplayName;
                if (GUILayout.Button(name, EditorStyles.miniButton, GUILayout.Width(130f)) && check.Layout != null)
                    Selection.activeObject = check.Layout;
                var style = check.IsSound ? EditorStyles.miniLabel : ShortStyle;
                GUILayout.Label(new GUIContent(check.Summary, check.Summary), style);
            }
        }

        // Every layout of `place` (or the place alone when it has none), with a progress bar.
        public static List<RoomLayoutCheck> CheckAll(LevelBuildSettings settings, RoomFunction place, int seeds)
        {
            var checks = new List<RoomLayoutCheck>();
            try
            {
                if (place.Layouts.Count == 0) checks.Add(RoomLayoutCheck.Run(settings, place, null, seeds));
                for (int i = 0; i < place.Layouts.Count; i++)
                {
                    RoomLayout layout = place.Layouts[i];
                    if (layout == null) continue;
                    EditorUtility.DisplayProgressBar("Checking layouts", $"{place.DisplayName}: {layout.DisplayName}", (float)i / place.Layouts.Count);
                    checks.Add(RoomLayoutCheck.Run(settings, place, layout, seeds));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            return checks;
        }

        private static string Label(RoomLayoutCheckPiece piece) => piece.Placement.Required ? $"{piece.Name} (required)" : piece.Name;

        private static void Row(GUIStyle style, GUIContent name, string from, string fits, string count)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(name, style);
                GUILayout.Label(from, style, GUILayout.Width(FromWidth));
                GUILayout.Label(fits, style, GUILayout.Width(FitsWidth));
                GUILayout.Label(count, style, GUILayout.Width(CountWidth));
            }
        }

        private static GUIStyle ShortStyle => _short ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.9f, 0.35f, 0.3f) },
        };
    }
}
