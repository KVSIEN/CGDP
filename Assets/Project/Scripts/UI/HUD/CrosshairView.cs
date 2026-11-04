using UnityEngine;
using UnityEngine.UI;
using CGD.Settings;

namespace CGD.UI
{
    // Draws a crosshair from a CrosshairStyle: four arms, a centre dot, and an outline behind
    // each. The HUD crosshair and the settings preview both use it, so they always agree.
    // Plain class, not a component: the owner gives it a RectTransform to draw into.
    public sealed class CrosshairView
    {
        // Order matters: index i of the arrays is this arm.
        private static readonly CrosshairArms[] ArmFlags =
            { CrosshairArms.Top, CrosshairArms.Bottom, CrosshairArms.Left, CrosshairArms.Right };

        private readonly Image[] _arms        = new Image[4];
        private readonly Image[] _armOutlines = new Image[4];
        private readonly Image   _dot;
        private readonly Image   _dotOutline;

        private CrosshairStyle _style = new();
        private float _spreadPixels;

        public CrosshairView(RectTransform parent)
        {
            // Outlines first so every fill draws on top of every outline.
            for (int i = 0; i < 4; i++) _armOutlines[i] = CreateImage(parent, "ArmOutline_" + i);
            _dotOutline = CreateImage(parent, "DotOutline");
            for (int i = 0; i < 4; i++) _arms[i] = CreateImage(parent, "Arm_" + i);
            _dot = CreateImage(parent, "Dot");
        }

        public void Apply(CrosshairStyle style)
        {
            _style = style;
            Draw();
        }

        // Extra distance of every arm from the centre, in pixels (the weapon's spread).
        public void SetSpread(float pixels)
        {
            if (Mathf.Approximately(pixels, _spreadPixels)) return;

            _spreadPixels = pixels;
            Place();
        }

        private static Image CreateImage(RectTransform parent, string name)
        {
            Image image = UIFactory.MakeImage(name, parent);
            RectTransform rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            return image;
        }

        private void Draw()
        {
            float edge   = _style.OutlineSize;
            Color fill   = _style.FillColor();
            Color border = _style.EdgeColor();

            for (int i = 0; i < 4; i++)
            {
                Vector2 size = ArmSize(i);
                bool shown = _style.Has(ArmFlags[i]);

                _arms[i].enabled = shown;
                _arms[i].color = fill;
                _arms[i].rectTransform.sizeDelta = size;

                _armOutlines[i].enabled = shown && edge > 0f;
                _armOutlines[i].color = border;
                _armOutlines[i].rectTransform.sizeDelta = size + Vector2.one * (edge * 2f);
            }

            Vector2 dotSize = new(_style.DotWidth, _style.DotHeight);
            _dot.enabled = _style.ShowDot;
            _dot.color = fill;
            _dot.rectTransform.sizeDelta = dotSize;

            _dotOutline.enabled = _style.ShowDot && edge > 0f;
            _dotOutline.color = border;
            _dotOutline.rectTransform.sizeDelta = dotSize + Vector2.one * (edge * 2f);

            Place();
        }

        // Top and bottom arms are tall and thin; left and right are wide and flat.
        private Vector2 ArmSize(int arm) => arm < 2
            ? new Vector2(_style.VerticalThickness, _style.VerticalLength)
            : new Vector2(_style.HorizontalLength, _style.HorizontalThickness);

        private void Place()
        {
            float up    = _style.GapY + _style.VerticalLength   * 0.5f + _spreadPixels;
            float across = _style.GapX + _style.HorizontalLength * 0.5f + _spreadPixels;

            SetPosition(0, new Vector2(0f,  up));
            SetPosition(1, new Vector2(0f, -up));
            SetPosition(2, new Vector2(-across, 0f));
            SetPosition(3, new Vector2( across, 0f));
        }

        private void SetPosition(int arm, Vector2 position)
        {
            _arms[arm].rectTransform.anchoredPosition        = position;
            _armOutlines[arm].rectTransform.anchoredPosition = position;
        }
    }
}
