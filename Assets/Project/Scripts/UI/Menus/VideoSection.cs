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
        private static readonly AdsZoomMode[] AdsZooms = { AdsZoomMode.Gradual, AdsZoomMode.Snap };
        private static readonly string[] AdsZoomLabels = { "Gradual", "Snap" };
        private static readonly AdsFovMode[] AdsFovs = { AdsFovMode.Independent, AdsFovMode.Affected };
        private static readonly string[] AdsFovLabels  = { "Independent", "Affected" };

        private readonly SettingsBinder _rows;
        private readonly OptionCycler _resolution;
        private readonly List<Vector2Int> _resolutions = new();

        public VideoSection(RectTransform page, Action changed)
        {
            var layout = new SettingsPageLayout(page);
            _rows = new SettingsBinder(layout, changed);

            layout.Header("Display");
            // Resolution is kept on reset: there's no sensible default beyond "what the monitor has now".
            _resolution = layout.Options("Resolution", Array.Empty<string>(), i => _rows.Change(d =>
            {
                d.ResolutionWidth  = _resolutions[i].x;
                d.ResolutionHeight = _resolutions[i].y;
            }));
            _rows.Bind(LoadResolutions, reset: null);
            _rows.Options("Window Mode", WindowModeLabels, WindowModes, d => d.WindowMode, (d, v) => d.WindowMode = v);
            AddQuality(layout);
            _rows.Toggle("VSync", d => d.VSync, (d, on) => d.VSync = on);
            _rows.Options("Frame Rate Limit", FrameRateLabels, FrameRateLimits, d => d.FrameRateLimit, (d, v) => d.FrameRateLimit = v);

            layout.Header("Camera");
            _rows.Slider("Field of View", SettingsData.MinFieldOfView, SettingsData.MaxFieldOfView,
                v => $"{Mathf.RoundToInt(v)}°", d => d.FieldOfView, (d, v) => d.FieldOfView = Mathf.Round(v));
            _rows.Options("ADS Zoom",          AdsZoomLabels, AdsZooms, d => d.AdsZoom, (d, v) => d.AdsZoom = v);
            _rows.Options("ADS Field of View", AdsFovLabels,  AdsFovs,  d => d.AdsFov,  (d, v) => d.AdsFov  = v);
        }

        public void Load(SettingsData draft) => _rows.Load(draft);

        public void ResetToDefaults() => _rows.ResetToDefaults();

        // Unset (-1) shows the level the game is running at now, and reset puts it back to unset.
        private void AddQuality(SettingsPageLayout layout)
        {
            OptionCycler quality = layout.Options("Quality", QualitySettings.names, i => _rows.Change(d => d.QualityLevel = i));
            _rows.Bind(d => quality.SetIndexWithoutNotify(d.QualityLevel >= 0 ? d.QualityLevel : QualitySettings.GetQualityLevel()),
                (defaults, draft) => draft.QualityLevel = defaults.QualityLevel);
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
    }
}
