using System;
using UnityEngine;
using CGD.Settings;

namespace CGD.UI
{
    // Volume sliders of the SettingsMenu. Changes are heard straight away.
    public class AudioSection
    {
        private readonly SettingsBinder _rows;

        public AudioSection(RectTransform page, Action changed)
        {
            var layout = new SettingsPageLayout(page);
            _rows = new SettingsBinder(layout, changed);

            layout.Header("Volume");
            _rows.Slider("Master",    0f, 1f, SettingsPageLayout.Percent, d => d.MasterVolume,    (d, v) => d.MasterVolume    = v);
            _rows.Slider("Effects",   0f, 1f, SettingsPageLayout.Percent, d => d.EffectsVolume,   (d, v) => d.EffectsVolume   = v);
            _rows.Slider("Music",     0f, 1f, SettingsPageLayout.Percent, d => d.MusicVolume,     (d, v) => d.MusicVolume     = v);
            _rows.Slider("Interface", 0f, 1f, SettingsPageLayout.Percent, d => d.InterfaceVolume, (d, v) => d.InterfaceVolume = v);
        }

        public void Load(SettingsData draft) => _rows.Load(draft);

        public void ResetToDefaults() => _rows.ResetToDefaults();
    }
}
