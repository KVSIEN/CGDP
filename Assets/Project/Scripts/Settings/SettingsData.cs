using System;
using UnityEngine;
using CGD.Difficulty;

namespace CGD.Settings
{
    // Player preferences outside of key bindings: audio, video, accessibility and input assists. Stored as JSON,
    // so a field added later simply keeps its default in older saves.
    [Serializable]
    public class SettingsData
    {
        // Audio (0..1)
        public float MasterVolume    = 1f;
        public float EffectsVolume   = 1f;
        public float MusicVolume     = 0.8f;
        public float InterfaceVolume = 1f;

        // Video
        // 0 = keep the current resolution (first launch uses whatever the player launched with).
        public int  ResolutionWidth;
        public int  ResolutionHeight;
        public FullScreenMode WindowMode = FullScreenMode.FullScreenWindow;
        // -1 = keep the project's default quality level.
        public int  QualityLevel = -1;
        public bool VSync        = true;
        // 0 = unlimited (only used while VSync is off).
        public int  FrameRateLimit;
        public float FieldOfView = 70f;
        public AdsZoomMode AdsZoom = AdsZoomMode.Gradual;
        public AdsFovMode  AdsFov  = AdsFovMode.Independent;

        // Gameplay
        public DifficultyLevel Difficulty = DifficultyLevel.Normal;

        // Accessibility (0..1 scales)
        public float CameraShake    = 1f;
        public float FlashIntensity = 1f;
        public bool  DamageVignette = true;
        // Arrows on screen pointing at whoever hurt the player.
        public bool  DamageIndicators = true;

        // The crosshair's look; see CrosshairStyle.
        public CrosshairStyle Crosshair = new();

        // Input assist: presses made while busy (drawing, dodging, between shots) happen as
        // soon as possible instead of being dropped. Off by default, like most shooters.
        public bool InputBuffering;
        // Holding an instant item's slot key opens the item wheel. The item is used on key
        // down, so opening the wheel this way also uses one; turn off to never do that.
        public bool InstantItemWheel = true;

        public const float MinFieldOfView = 60f;
        public const float MaxFieldOfView = 110f;

        // A copy the settings menu can edit without touching the live settings.
        public SettingsData Clone()
        {
            var copy = (SettingsData)MemberwiseClone();
            copy.Crosshair = Crosshair.Clone();
            return copy;
        }
    }
}
