using System.Collections.Generic;
using CGD.Core;
using CGD.Level;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A strip of the same recipe built from the next seeds (SeedSequence: 7 → 8, 9…), so a
    // designer sees the range a room function produces instead of one roll at a time, and can
    // spot the odd one out. Rooms are built a few per editor tick; clicking one uses its seed.
    public class RoomLabVariations
    {
        public const float Height = 128f;
        private const int Count = 8;
        private const double TickBudget = 0.03;

        private sealed class Entry
        {
            public string Seed;
            public RoomSample Sample;
            public readonly RoomPlanTexture Plan = new();
            public bool Built;
        }

        private readonly List<Entry> _entries = new();
        private RoomLabRecipe _recipe;
        private int _next;

        // Starts over for `recipe` (null: nothing to show).
        public void Reset(RoomLabRecipe recipe)
        {
            Release();
            _recipe = recipe != null && recipe.Source == RoomLabRecipe.SourceKind.Recipe ? recipe.Clone() : null;
            _next = 0;
            if (_recipe == null) return;

            bool first = true;
            foreach (string seed in SeedSequence.CountingFrom(_recipe.Seed, Count + 1))
            {
                if (first) { first = false; continue; }   // the room on show
                _entries.Add(new Entry { Seed = seed });
            }
        }

        // Builds what fits in a tick; true when something new needs drawing.
        public bool BuildSome(LevelBlueprint.Fill fill)
        {
            if (_recipe == null || _next >= _entries.Count) return false;

            double stop = EditorApplication.timeSinceStartup + TickBudget;
            do
            {
                Entry entry = _entries[_next++];
                entry.Built  = true;
                entry.Sample = RoomSampleBuilder.Build(_recipe, entry.Seed);
                if (entry.Sample.IsValid) entry.Plan.Render(entry.Sample, fill);
            }
            while (_next < _entries.Count && EditorApplication.timeSinceStartup < stop);
            return true;
        }

        // Returns the seed clicked, or null.
        public string Draw(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
            if (_recipe == null)
            {
                GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 18f),
                          "Variations show for made-up rooms: a map room always uses its graph's seed.", EditorStyles.miniLabel);
                return null;
            }

            GUI.Label(new Rect(rect.x + 8f, rect.y + 2f, rect.width, 16f), "Next seeds — click one to use it", EditorStyles.miniBoldLabel);
            float width = (rect.width - 8f) / Count;
            string picked = null;
            for (int i = 0; i < _entries.Count; i++)
            {
                var cell = new Rect(rect.x + 4f + i * width, rect.y + 18f, width - 4f, rect.height - 22f);
                if (DrawEntry(cell, _entries[i])) picked = _entries[i].Seed;
            }
            return picked;
        }

        private static bool DrawEntry(Rect cell, Entry entry)
        {
            EditorGUI.DrawRect(cell, LevelBlueprint.Background);
            var image = new Rect(cell.x + 2f, cell.y + 2f, cell.width - 4f, cell.height - 20f);
            string caption = entry.Seed;
            if (!entry.Built) caption += " …";
            else if (!entry.Sample.IsValid) caption += " · failed";
            else
            {
                entry.Plan.Draw(entry.Plan.Fit(image));
                int problems = 0;
                foreach (RoomIssue issue in entry.Sample.Issues)
                    if (issue.Level != RoomIssue.Severity.Info) problems++;
                caption += $" · {entry.Sample.Room.DisplayName}" + (problems > 0 ? $" · {problems}⚠" : "");
            }
            GUI.Label(new Rect(cell.x + 2f, cell.yMax - 17f, cell.width - 4f, 16f), new GUIContent(caption, caption), EditorStyles.whiteMiniLabel);

            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !cell.Contains(e.mousePosition) || !entry.Built) return false;
            e.Use();
            return true;
        }

        public void Release()
        {
            foreach (Entry entry in _entries) entry.Plan.Release();
            _entries.Clear();
        }
    }
}
