using System;
using UnityEngine;

namespace CGD.Settings
{
    // How the player's crosshair looks. Sizes are in HUD pixels (the HUD scales them with the
    // screen). The horizontal arms (left, right) and the vertical arms (top, bottom) are sized
    // independently, as are the gaps and the centre dot, so a crosshair can be wider than it
    // is tall. Stored with the other settings as JSON; a field added later keeps its default.
    [Serializable]
    public class CrosshairStyle
    {
        public CrosshairArms Arms = CrosshairArms.All;

        // Left and right arms: length along X, thickness along Y.
        public float HorizontalLength    = 6f;
        public float HorizontalThickness = 2f;
        // Top and bottom arms: length along Y, thickness along X.
        public float VerticalLength      = 6f;
        public float VerticalThickness   = 2f;
        // Distance from the centre to the inner end of each arm.
        public float GapX = 4f;
        public float GapY = 4f;

        public bool  ShowDot;
        public float DotWidth  = 4f;
        public float DotHeight = 4f;

        public Color Tint    = Color.white;
        [Range(0f, 1f)] public float Opacity = 1f;

        public bool  Outline;
        public Color OutlineColor     = Color.black;
        public float OutlineThickness = 1f;

        // How strongly the arms spread apart with the weapon's spread: 0 = fixed, 1 = true to
        // the weapon, 2 = exaggerated.
        public float SpreadResponse = 1f;

        public const float MaxLength    = 40f;
        public const float MaxThickness = 10f;
        public const float MaxGap       = 30f;
        public const float MaxDot       = 20f;
        public const float MaxOutline   = 5f;
        public const float MaxSpread    = 2f;

        public bool Has(CrosshairArms arm) => (Arms & arm) != 0;

        // The fill colour with the opacity applied.
        public Color FillColor() => new(Tint.r, Tint.g, Tint.b, Opacity);

        public Color EdgeColor() => new(OutlineColor.r, OutlineColor.g, OutlineColor.b, Opacity);

        public float OutlineSize => Outline ? OutlineThickness : 0f;

        public CrosshairStyle Clone() => (CrosshairStyle)MemberwiseClone();
    }
}
