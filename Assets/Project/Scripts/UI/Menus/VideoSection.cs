using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Settings;

namespace CGD.UI
{
    // Display options of the SettingsMenu. Window changes only take effect in builds —
    // the editor's Game view keeps its own size.
    public class VideoSection
    {
        private static readonly FullScreenMode[] WindowModes =
            { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
        private static readonly string[] WindowModeLabels = { "Fullscreen", "Borderless", "Windowed" };

        private static readonly int[] FrameRateLimits = { 0, 30, 60, 120, 144, 165, 240 };
        private static readonly string[] FrameRateLabels = { "Unlimited", "30", "60", "120", "144", "165", "240" };

        private readonly Action _changed;

        private readonly OptionCycler  _resolution;
        private readonly OptionCycler  _windowMode;
        private readonly OptionCycler  _quality;
        private readonly OptionCycler  _vsync;
        private readonly OptionCycler  _frameRate;
        private readonly LabeledSlider _fov;

        private readonly List<Vector2Int> _resolutions = new();
        private SettingsData _draft;

        public VideoSection(RectTransform page, Action changed)
        {
            _changed = changed;

            var layout = new SettingsPageLayout(page);
            layout.Header("Display");
            _resolution = layout.Options("Resolution", Array.Empty<string>(), i => Set(d =>
            {
                d.ResolutionWidth  = _resolutions[i].x;
                d.ResolutionHeight = _resolutions[i].y;
            }));
            _windowMode = layout.Options("Window Mode", WindowModeLabels, i => Set(d => d.WindowMode = WindowModes[i]));
            _quality    = layout.Options("Quality", QualitySettings.names, i => Set(d => d.QualityLevel = i));
            _vsync      = layout.Toggle("VSync", on => Set(d => d.VSync = on));
            _frameRate  = layout.Options("Frame Rate Limit", FrameRateLabels, i => Set(d => d.FrameRateLimit = FrameRateLimits[i]));

            layout.Header("Camera");
            _fov = layout.Slider("Field of View", SettingsData.MinFieldOfView, SettingsData.MaxFieldOfView,
                v => $"{Mathf.RoundToInt(v)}°", v => Set(d => d.FieldOfView = Mathf.Round(v)));
        }

        public void Load(SettingsData draft)
        {
            _draft = draft;
            LoadResolutions(draft);
            _windowMode.SetIndexWithoutNotify(Array.IndexOf(WindowModes, draft.WindowMode));
            _quality.SetIndexWithoutNotify(draft.QualityLevel >= 0 ? draft.QualityLevel : QualitySettings.GetQualityLevel());
            _vsync.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(draft.VSync));
            _frameRate.SetIndexWithoutNotify(Math.Max(0, Array.IndexOf(FrameRateLimits, draft.FrameRateLimit)));
            _fov.SetValueWithoutNotify(draft.FieldOfView);
        }

        public void ResetToDefaults()
        {
            var defaults = new SettingsData();
            _draft.WindowMode     = defaults.WindowMode;
            _draft.QualityLevel   = defaults.QualityLevel;
            _draft.VSync          = defaults.VSync;
            _draft.FrameRateLimit = defaults.FrameRateLimit;
            _draft.FieldOfView    = defaults.FieldOfView;
            // Resolution stays: there's no sensible default beyond "what the monitor has now".
            Load(_draft);
            _changed();
        }

        // Screen.resolutions lists every refresh rate separately; the menu only picks a size.
        private void LoadResolutions(SettingsData draft)
        {
            _resolutions.Clear();
            foreach (var r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!_resolutions.Contains(size)) _resolutions.Add(size);
            }

            var current = new Vector2Int(
                draft.ResolutionWidth  > 0 ? draft.ResolutionWidth  : Screen.width,
                draft.ResolutionHeight > 0 ? draft.ResolutionHeight : Screen.height);
            if (!_resolutions.Contains(current)) _resolutions.Add(current);
            _resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            var labels = new string[_resolutions.Count];
            for (int i = 0; i < labels.Length; i++) labels[i] = $"{_resolutions[i].x} × {_resolutions[i].y}";
            _resolution.SetOptions(labels, _resolutions.IndexOf(current));
        }

        private void Set(Action<SettingsData> change)
        {
            change(_draft);
            _changed();
        }
    }
}
