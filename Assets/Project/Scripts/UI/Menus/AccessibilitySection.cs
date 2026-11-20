using System;
using UnityEngine;
using CGD.Settings;

namespace CGD.UI
{
    // Difficulty plus the comfort and input-assist options of the SettingsMenu: how much the camera moves on
    // its own, how strong full-screen flashes are, and input buffering. Hold vs. toggle per
    // action lives on the Controls tab.
    public class AccessibilitySection
    {
        private readonly SettingsBinder _rows;

        public AccessibilitySection(RectTransform page, Action changed)
        {
            var layout = new SettingsPageLayout(page);
            _rows = new SettingsBinder(layout, changed);

            layout.Header("Gameplay");
            _rows.Options("Difficulty", d => d.Difficulty, (d, v) => d.Difficulty = v);

            layout.Header("Comfort");
            _rows.Slider("Camera Shake & Kicks", 0f, 1f, SettingsPageLayout.Percent, d => d.CameraShake,    (d, v) => d.CameraShake    = v);
            _rows.Slider("Screen Flashes",       0f, 1f, SettingsPageLayout.Percent, d => d.FlashIntensity, (d, v) => d.FlashIntensity = v);
            _rows.Toggle("Low Health Vignette",     d => d.DamageVignette,   (d, on) => d.DamageVignette   = on);
            _rows.Toggle("Damage Direction Arrows", d => d.DamageIndicators, (d, on) => d.DamageIndicators = on);

            layout.Header("Input");
            _rows.Toggle("Input Buffering",             d => d.InputBuffering,   (d, on) => d.InputBuffering   = on);
            _rows.Toggle("Hold Instant Item for Wheel", d => d.InstantItemWheel, (d, on) => d.InstantItemWheel = on);
        }

        public void Load(SettingsData draft) => _rows.Load(draft);

        public void ResetToDefaults() => _rows.ResetToDefaults();
    }
}
