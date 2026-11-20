using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Draws a sweep's expressive range in the Room Sweep window: one square per faction, the
    // rooms on two chosen measures, darker where more of them fall, with each square's spread
    // and its overlap with the first faction's.
    public sealed class ExpressiveRangePlot
    {
        private const float Size = 160f, Gap = 16f;
        private static readonly Color Empty = new(0.5f, 0.5f, 0.5f, 0.12f);
        private static readonly Color Full  = new(0.95f, 0.55f, 0.15f, 1f);

        private RoomMeasure _x = RoomSweepReport.Plots[0].X;
        private RoomMeasure _y = RoomSweepReport.Plots[0].Y;

        public void Draw(IReadOnlyList<RoomSweepResult> results, float width)
        {
            if (results.Count == 0) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                _x = (RoomMeasure)EditorGUILayout.EnumPopup(new GUIContent("Across", "The measure along the bottom"), _x);
                _y = (RoomMeasure)EditorGUILayout.EnumPopup(new GUIContent("Up", "The measure up the side"), _y);
            }

            List<(string Faction, ExpressiveRange Range)> ranges = results.GroupBy(r => r.Faction)
                .Select(f => (f.Key, new ExpressiveRange(f, _x, _y))).ToList();
            int perRow = Mathf.Max(1, Mathf.FloorToInt((width - Gap) / (Size + Gap)));
            for (int start = 0; start < ranges.Count; start += perRow)
            {
                Rect row = GUILayoutUtility.GetRect(width, Size + 40f);
                for (int i = start; i < Mathf.Min(start + perRow, ranges.Count); i++)
                {
                    var square = new Rect(row.x + Gap + (i - start) * (Size + Gap), row.y + 18f, Size, Size);
                    DrawSquare(square, ranges[i].Range);
                    string overlap = i > 0 ? $", {ExpressiveRange.Overlap(ranges[0].Range, ranges[i].Range):0%} like {ranges[0].Faction}" : "";
                    GUI.Label(new Rect(square.x, row.y, Size + Gap, 18f), $"{ranges[i].Faction}: spread {ranges[i].Range.Coverage:0%}{overlap}", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.LabelField($"Across: {RoomMeasures.Name(_x)} · up: {RoomMeasures.Name(_y)} · {ExpressiveRange.Bins}×{ExpressiveRange.Bins} cells",
                                       EditorStyles.miniLabel);
        }

        private static void DrawSquare(Rect square, ExpressiveRange range)
        {
            float cell = square.width / ExpressiveRange.Bins;
            for (int x = 0; x < ExpressiveRange.Bins; x++)
                for (int y = 0; y < ExpressiveRange.Bins; y++)
                {
                    int count = range.Count(x, y);
                    Color color = count == 0 ? Empty : Color.Lerp(Full * new Color(1f, 1f, 1f, 0.25f), Full, count / (float)range.Max);
                    // y grows downward on screen, so the first row is drawn at the bottom.
                    var rect = new Rect(square.x + x * cell, square.yMax - (y + 1) * cell, cell - 1f, cell - 1f);
                    EditorGUI.DrawRect(rect, color);
                }
        }
    }
}
