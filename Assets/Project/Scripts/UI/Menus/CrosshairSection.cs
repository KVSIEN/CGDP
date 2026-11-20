using System;
using UnityEngine;
using UnityEngine.UI;
using CGD.Settings;

namespace CGD.UI
{
    // The crosshair page of the SettingsMenu: arm sizes per axis, gaps, centre dot, colour,
    // opacity, outline and how much it reacts to weapon spread, with a live preview. Edits
    // go to the draft's CrosshairStyle; the menu applies the draft after every change.
    public class CrosshairSection
    {
        private const float ColumnWidth   = 310f;
        private const float LabelWidth    = 105f;
        private const float ControlWidth  = ColumnWidth - LabelWidth;
        private const float LeftColumn    = 10f;
        private const float RightColumn   = 345f;
        private const float PreviewHeight = 120f;

        private static readonly CrosshairArms[] ArmPresets =
        {
            CrosshairArms.All,
            CrosshairArms.All & ~CrosshairArms.Top,
            CrosshairArms.All & ~CrosshairArms.Bottom,
            CrosshairArms.Horizontal,
            CrosshairArms.Vertical,
            CrosshairArms.None,
        };
        private static readonly string[] ArmLabels = { "All", "No Top", "No Bottom", "Horizontal", "Vertical", "None" };

        private readonly Action _changed;
        private readonly SettingsBinder _rows;
        private readonly CrosshairView _preview;

        public CrosshairSection(RectTransform page, Action changed)
        {
            _changed = changed;

            var left = new SettingsPageLayout(page, LeftColumn, LabelWidth, ControlWidth);
            // Both columns write to the one binder, so every row loads together; each edit redraws the preview.
            _rows = new SettingsBinder(left, () => { _preview.Apply(_rows.Draft.Crosshair); _changed(); });
            left.Header("Lines");
            _rows.Options("Arms", ArmLabels, ArmPresets, d => d.Crosshair.Arms, (d, v) => d.Crosshair.Arms = v);
            WholeSlider(_rows, "Length X",    CrosshairStyle.MaxLength,    s => s.HorizontalLength,    (s, v) => s.HorizontalLength    = v);
            WholeSlider(_rows, "Thickness X", CrosshairStyle.MaxThickness, s => s.HorizontalThickness, (s, v) => s.HorizontalThickness = v);
            WholeSlider(_rows, "Length Y",    CrosshairStyle.MaxLength,    s => s.VerticalLength,      (s, v) => s.VerticalLength      = v);
            WholeSlider(_rows, "Thickness Y", CrosshairStyle.MaxThickness, s => s.VerticalThickness,   (s, v) => s.VerticalThickness   = v);
            WholeSlider(_rows, "Gap X",       CrosshairStyle.MaxGap,       s => s.GapX,                (s, v) => s.GapX                = v);
            WholeSlider(_rows, "Gap Y",       CrosshairStyle.MaxGap,       s => s.GapY,                (s, v) => s.GapY                = v);

            left.Header("Center Dot");
            _rows.Toggle("Dot", d => d.Crosshair.ShowDot, (d, on) => d.Crosshair.ShowDot = on);
            WholeSlider(_rows, "Dot Width",  CrosshairStyle.MaxDot, s => s.DotWidth,  (s, v) => s.DotWidth  = v);
            WholeSlider(_rows, "Dot Height", CrosshairStyle.MaxDot, s => s.DotHeight, (s, v) => s.DotHeight = v);

            var right = new SettingsPageLayout(page, RightColumn, LabelWidth, ControlWidth);
            var rightRows = _rows.On(right);
            right.Header("Color");
            rightRows.Slider("Opacity", 0.1f, 1f, SettingsPageLayout.Percent, d => d.Crosshair.Opacity, (d, v) => d.Crosshair.Opacity = v);
            rightRows.Slider("Red",   0f, 1f, Channel, d => d.Crosshair.Tint.r, (d, v) => d.Crosshair.Tint = new Color(v, d.Crosshair.Tint.g, d.Crosshair.Tint.b));
            rightRows.Slider("Green", 0f, 1f, Channel, d => d.Crosshair.Tint.g, (d, v) => d.Crosshair.Tint = new Color(d.Crosshair.Tint.r, v, d.Crosshair.Tint.b));
            rightRows.Slider("Blue",  0f, 1f, Channel, d => d.Crosshair.Tint.b, (d, v) => d.Crosshair.Tint = new Color(d.Crosshair.Tint.r, d.Crosshair.Tint.g, v));

            right.Header("Outline");
            rightRows.Toggle("Outline", d => d.Crosshair.Outline, (d, on) => d.Crosshair.Outline = on);
            WholeSlider(rightRows, "Outline Size", CrosshairStyle.MaxOutline, s => s.OutlineThickness, (s, v) => s.OutlineThickness = v);

            right.Header("Behavior");
            rightRows.Slider("Spread", 0f, CrosshairStyle.MaxSpread, SettingsPageLayout.Percent,
                d => d.Crosshair.SpreadResponse, (d, v) => d.Crosshair.SpreadResponse = v);

            _preview = BuildPreview(page, right.Cursor + new Vector2(0f, -8f));
            _rows.Bind(d => _preview.Apply(d.Crosshair), reset: null);
        }

        public void Load(SettingsData draft) => _rows.Load(draft);

        // The whole style goes back to its defaults, including what has no row (the outline colour).
        public void ResetToDefaults()
        {
            _rows.Draft.Crosshair = new CrosshairStyle();
            _rows.Load(_rows.Draft);
            _changed();
        }

        // A crosshair size in whole pixels, down the given column.
        private static void WholeSlider(SettingsBinder column, string label, float max,
            Func<CrosshairStyle, float> get, Action<CrosshairStyle, float> set) =>
            column.Slider(label, 0f, max, Whole, d => get(d.Crosshair), (d, v) => set(d.Crosshair, Mathf.Round(v)));

        private static string Whole(float value) => Mathf.RoundToInt(value).ToString();

        private static string Channel(float value) => Mathf.RoundToInt(value * 255f).ToString();

        // Half dark, half light, so the outline can be judged against both.
        private static CrosshairView BuildPreview(RectTransform page, Vector2 topLeft)
        {
            Image dark = UIFactory.MakeImage("PreviewDark", page);
            dark.color = new Color(0.12f, 0.13f, 0.15f, 1f);
            UIFactory.Place(dark.rectTransform, topLeft, new Vector2(ColumnWidth * 0.5f, PreviewHeight));

            Image light = UIFactory.MakeImage("PreviewLight", page);
            light.color = new Color(0.78f, 0.8f, 0.82f, 1f);
            UIFactory.Place(light.rectTransform, topLeft + new Vector2(ColumnWidth * 0.5f, 0f), new Vector2(ColumnWidth * 0.5f, PreviewHeight));

            var center = new GameObject("PreviewCrosshair", typeof(RectTransform));
            center.transform.SetParent(page, false);
            var rt = (RectTransform)center.transform;
            UIFactory.Place(rt, topLeft, new Vector2(ColumnWidth, PreviewHeight));
            return new CrosshairView(rt);
        }
    }
}
