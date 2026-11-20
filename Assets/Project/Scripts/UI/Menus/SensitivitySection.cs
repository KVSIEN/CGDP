using System;
using UnityEngine;
using CGD.Settings;

namespace CGD.UI
{
    // Mouse and gamepad look sensitivity at the top of the SettingsMenu's Controls tab. Like the
    // other pages, edits apply live and are saved when the menu closes.
    public class SensitivitySection
    {
        private readonly SettingsBinder _rows;

        public SensitivitySection(RectTransform page, Action changed)
        {
            var layout = new SettingsPageLayout(page);
            _rows = new SettingsBinder(layout, changed);

            layout.Header("Sensitivity");
            _rows.Slider("Mouse", SettingsData.MinMouseSensitivity, SettingsData.MaxMouseSensitivity, v => v.ToString("F2"),
                         d => d.MouseSensitivity, (d, v) => d.MouseSensitivity = v);
            _rows.Slider("Gamepad", SettingsData.MinGamepadSensitivity, SettingsData.MaxGamepadSensitivity, v => v.ToString("F0"),
                         d => d.GamepadSensitivity, (d, v) => d.GamepadSensitivity = v);
        }

        public void Load(SettingsData draft) => _rows.Load(draft);

        public void ResetToDefaults() => _rows.ResetToDefaults();
    }
}
