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
        private readonly CrosshairView _preview;

        private readonly OptionCycler  _arms;
        private readonly LabeledSlider _lengthX;
        private readonly LabeledSlider _thicknessX;
        private readonly LabeledSlider _lengthY;
        private readonly LabeledSlider _thicknessY;
        private readonly LabeledSlider _gapX;
        private readonly LabeledSlider _gapY;
        private readonly OptionCycler  _dot;
        private readonly LabeledSlider _dotWidth;
        private readonly LabeledSlider _dotHeight;
        private readonly LabeledSlider _opacity;
        private readonly LabeledSlider _red;
        private readonly LabeledSlider _green;
        private readonly LabeledSlider _blue;
        private readonly OptionCycler  _outline;
        private readonly LabeledSlider _outlineSize;
        private readonly LabeledSlider _spread;

        private SettingsData _draft;

        public CrosshairSection(RectTransform page, Action changed)
        {
            _changed = changed;

            var left = new SettingsPageLayout(page, LeftColumn, LabelWidth, ControlWidth);
            left.Header("Lines");
            _arms       = left.Options("Arms", ArmLabels, i => Set(s => s.Arms = ArmPresets[i]));
            _lengthX    = left.Slider("Length X",    0f, CrosshairStyle.MaxLength,    Whole, v => Set(s => s.HorizontalLength    = Mathf.Round(v)));
            _thicknessX = left.Slider("Thickness X", 0f, CrosshairStyle.MaxThickness, Whole, v => Set(s => s.HorizontalThickness = Mathf.Round(v)));
            _lengthY    = left.Slider("Length Y",    0f, CrosshairStyle.MaxLength,    Whole, v => Set(s => s.VerticalLength      = Mathf.Round(v)));
            _thicknessY = left.Slider("Thickness Y", 0f, CrosshairStyle.MaxThickness, Whole, v => Set(s => s.VerticalThickness   = Mathf.Round(v)));
            _gapX       = left.Slider("Gap X",       0f, CrosshairStyle.MaxGap,       Whole, v => Set(s => s.GapX                = Mathf.Round(v)));
            _gapY       = left.Slider("Gap Y",       0f, CrosshairStyle.MaxGap,       Whole, v => Set(s => s.GapY                = Mathf.Round(v)));

            left.Header("Center Dot");
            _dot       = left.Toggle("Dot", on => Set(s => s.ShowDot = on));
            _dotWidth  = left.Slider("Dot Width",  0f, CrosshairStyle.MaxDot, Whole, v => Set(s => s.DotWidth  = Mathf.Round(v)));
            _dotHeight = left.Slider("Dot Height", 0f, CrosshairStyle.MaxDot, Whole, v => Set(s => s.DotHeight = Mathf.Round(v)));

            var right = new SettingsPageLayout(page, RightColumn, LabelWidth, ControlWidth);
            right.Header("Color");
            _opacity = right.Slider("Opacity", 0.1f, 1f, SettingsPageLayout.Percent, v => Set(s => s.Opacity = v));
            _red     = right.Slider("Red",   0f, 1f, Channel, v => Set(s => s.Tint = new Color(v, s.Tint.g, s.Tint.b)));
            _green   = right.Slider("Green", 0f, 1f, Channel, v => Set(s => s.Tint = new Color(s.Tint.r, v, s.Tint.b)));
            _blue    = right.Slider("Blue",  0f, 1f, Channel, v => Set(s => s.Tint = new Color(s.Tint.r, s.Tint.g, v)));

            right.Header("Outline");
            _outline     = right.Toggle("Outline", on => Set(s => s.Outline = on));
            _outlineSize = right.Slider("Outline Size", 0f, CrosshairStyle.MaxOutline, Whole, v => Set(s => s.OutlineThickness = Mathf.Round(v)));

            right.Header("Behavior");
            _spread = right.Slider("Spread", 0f, CrosshairStyle.MaxSpread, SettingsPageLayout.Percent, v => Set(s => s.SpreadResponse = v));

            _preview = BuildPreview(page, right.Cursor + new Vector2(0f, -8f));
        }

        public void Load(SettingsData draft)
        {
            _draft = draft;
            CrosshairStyle s = draft.Crosshair;

            _arms.SetIndexWithoutNotify(Mathf.Max(0, Array.IndexOf(ArmPresets, s.Arms)));
            _lengthX.SetValueWithoutNotify(s.HorizontalLength);
            _thicknessX.SetValueWithoutNotify(s.HorizontalThickness);
            _lengthY.SetValueWithoutNotify(s.VerticalLength);
            _thicknessY.SetValueWithoutNotify(s.VerticalThickness);
            _gapX.SetValueWithoutNotify(s.GapX);
            _gapY.SetValueWithoutNotify(s.GapY);
            _dot.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(s.ShowDot));
            _dotWidth.SetValueWithoutNotify(s.DotWidth);
            _dotHeight.SetValueWithoutNotify(s.DotHeight);
            _opacity.SetValueWithoutNotify(s.Opacity);
            _red.SetValueWithoutNotify(s.Tint.r);
            _green.SetValueWithoutNotify(s.Tint.g);
            _blue.SetValueWithoutNotify(s.Tint.b);
            _outline.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(s.Outline));
            _outlineSize.SetValueWithoutNotify(s.OutlineThickness);
            _spread.SetValueWithoutNotify(s.SpreadResponse);

            _preview.Apply(s);
        }

        public void ResetToDefaults()
        {
            _draft.Crosshair = new CrosshairStyle();
            Load(_draft);
            _changed();
        }

        private void Set(Action<CrosshairStyle> change)
        {
            change(_draft.Crosshair);
            _preview.Apply(_draft.Crosshair);
            _changed();
        }

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
