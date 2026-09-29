using System;
using UnityEngine;
using CGD.Settings;

namespace CGD.UI
{
    // Volume sliders of the SettingsMenu. Changes are heard straight away.
    public class AudioSection
    {
        private readonly Action _changed;

        private readonly LabeledSlider _master;
        private readonly LabeledSlider _effects;
        private readonly LabeledSlider _music;
        private readonly LabeledSlider _interface;

        private SettingsData _draft;

        public AudioSection(RectTransform page, Action changed)
        {
            _changed = changed;

            var layout = new SettingsPageLayout(page);
            layout.Header("Volume");
            _master    = layout.Slider("Master",    0f, 1f, SettingsPageLayout.Percent, v => Set(d => d.MasterVolume    = v));
            _effects   = layout.Slider("Effects",   0f, 1f, SettingsPageLayout.Percent, v => Set(d => d.EffectsVolume   = v));
            _music     = layout.Slider("Music",     0f, 1f, SettingsPageLayout.Percent, v => Set(d => d.MusicVolume     = v));
            _interface = layout.Slider("Interface", 0f, 1f, SettingsPageLayout.Percent, v => Set(d => d.InterfaceVolume = v));
        }

        public void Load(SettingsData draft)
        {
            _draft = draft;
            _master.SetValueWithoutNotify(draft.MasterVolume);
            _effects.SetValueWithoutNotify(draft.EffectsVolume);
            _music.SetValueWithoutNotify(draft.MusicVolume);
            _interface.SetValueWithoutNotify(draft.InterfaceVolume);
        }

        public void ResetToDefaults()
        {
            var defaults = new SettingsData();
            _draft.MasterVolume    = defaults.MasterVolume;
            _draft.EffectsVolume   = defaults.EffectsVolume;
            _draft.MusicVolume     = defaults.MusicVolume;
            _draft.InterfaceVolume = defaults.InterfaceVolume;
            Load(_draft);
            _changed();
        }

        private void Set(Action<SettingsData> change)
        {
            change(_draft);
            _changed();
        }
    }
}
