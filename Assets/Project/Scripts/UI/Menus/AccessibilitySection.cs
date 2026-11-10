using System;
using UnityEngine;
using CGD.Difficulty;
using CGD.Settings;

namespace CGD.UI
{
    // Difficulty plus the comfort and input-assist options of the SettingsMenu: how much the camera moves on
    // its own, how strong full-screen flashes are, and input buffering. Hold vs. toggle per
    // action lives on the Controls tab.
    public class AccessibilitySection
    {
        private readonly Action _changed;

        private readonly OptionCycler  _difficulty;
        private readonly LabeledSlider _shake;
        private readonly LabeledSlider _flash;
        private readonly OptionCycler  _vignette;
        private readonly OptionCycler  _damageIndicators;
        private readonly OptionCycler  _buffering;
        private readonly OptionCycler  _instantItemWheel;

        private SettingsData _draft;

        public AccessibilitySection(RectTransform page, Action changed)
        {
            _changed = changed;

            var layout = new SettingsPageLayout(page);
            layout.Header("Gameplay");
            _difficulty = layout.Options("Difficulty", Enum.GetNames(typeof(DifficultyLevel)), i => Set(d => d.Difficulty = (DifficultyLevel)i));

            layout.Header("Comfort");
            _shake    = layout.Slider("Camera Shake & Kicks", 0f, 1f, SettingsPageLayout.Percent, v => Set(d => d.CameraShake    = v));
            _flash    = layout.Slider("Screen Flashes",       0f, 1f, SettingsPageLayout.Percent, v => Set(d => d.FlashIntensity = v));
            _vignette = layout.Toggle("Low Health Vignette", on => Set(d => d.DamageVignette = on));
            _damageIndicators = layout.Toggle("Damage Direction Arrows", on => Set(d => d.DamageIndicators = on));

            layout.Header("Input");
            _buffering        = layout.Toggle("Input Buffering", on => Set(d => d.InputBuffering = on));
            _instantItemWheel = layout.Toggle("Hold Instant Item for Wheel", on => Set(d => d.InstantItemWheel = on));
        }

        public void Load(SettingsData draft)
        {
            _draft = draft;
            _difficulty.SetIndexWithoutNotify((int)draft.Difficulty);
            _shake.SetValueWithoutNotify(draft.CameraShake);
            _flash.SetValueWithoutNotify(draft.FlashIntensity);
            _vignette.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(draft.DamageVignette));
            _damageIndicators.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(draft.DamageIndicators));
            _buffering.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(draft.InputBuffering));
            _instantItemWheel.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(draft.InstantItemWheel));
        }

        public void ResetToDefaults()
        {
            var defaults = new SettingsData();
            _draft.Difficulty     = defaults.Difficulty;
            _draft.CameraShake    = defaults.CameraShake;
            _draft.FlashIntensity = defaults.FlashIntensity;
            _draft.DamageVignette = defaults.DamageVignette;
            _draft.DamageIndicators = defaults.DamageIndicators;
            _draft.InputBuffering   = defaults.InputBuffering;
            _draft.InstantItemWheel = defaults.InstantItemWheel;
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
