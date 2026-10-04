using System;
using UnityEngine;
using CGD.Settings;

namespace CGD.UI
{
    // Comfort and input-assist options of the SettingsMenu: how much the camera moves on
    // its own, how strong full-screen flashes are, and input buffering. Hold vs. toggle per
    // action lives on the Controls tab.
    public class AccessibilitySection
    {
        private readonly Action _changed;

        private readonly LabeledSlider _shake;
        private readonly LabeledSlider _flash;
        private readonly OptionCycler  _vignette;
        private readonly OptionCycler  _buffering;

        private SettingsData _draft;

        public AccessibilitySection(RectTransform page, Action changed)
        {
            _changed = changed;

            var layout = new SettingsPageLayout(page);
            layout.Header("Comfort");
            _shake    = layout.Slider("Camera Shake & Kicks", 0f, 1f, SettingsPageLayout.Percent, v => Set(d => d.CameraShake    = v));
            _flash    = layout.Slider("Screen Flashes",       0f, 1f, SettingsPageLayout.Percent, v => Set(d => d.FlashIntensity = v));
            _vignette = layout.Toggle("Low Health Vignette", on => Set(d => d.DamageVignette = on));

            layout.Header("Input");
            _buffering = layout.Toggle("Input Buffering", on => Set(d => d.InputBuffering = on));
        }

        public void Load(SettingsData draft)
        {
            _draft = draft;
            _shake.SetValueWithoutNotify(draft.CameraShake);
            _flash.SetValueWithoutNotify(draft.FlashIntensity);
            _vignette.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(draft.DamageVignette));
            _buffering.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(draft.InputBuffering));
        }

        public void ResetToDefaults()
        {
            var defaults = new SettingsData();
            _draft.CameraShake    = defaults.CameraShake;
            _draft.FlashIntensity = defaults.FlashIntensity;
            _draft.DamageVignette = defaults.DamageVignette;
            _draft.InputBuffering = defaults.InputBuffering;
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
