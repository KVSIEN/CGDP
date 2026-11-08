using System;
using UnityEngine;

namespace CGD.DevTools
{
    // How this viewer likes the spectator camera: look feel, speed, smoothing, labels,
    // whether the help is showing, the inspector page and which world overlays are on.
    // Starts from SpectatorInputSettings' defaults and is kept in PlayerPrefs
    // (SpectatorSave), so it survives sessions.
    [Serializable]
    public class SpectatorPreferences
    {
        public float LookSensitivity;
        public bool  InvertLook;
        public float MoveSpeed;
        public float Smoothing;
        public float LabelScale;
        public bool  ShowLabels = true;
        public bool  ShowHelp   = true;   // shown the first time, so the controls are discoverable
        public InspectorPage InspectorPage = InspectorPage.Overview;
        // World overlays (see SpectatorOverlays); senses is on at first, so they're discoverable.
        public bool  ShowSenses = true;
        public bool  ShowCover;
        public bool  ShowPlans;

        public static SpectatorPreferences DefaultsFrom(SpectatorInputSettings settings) => new()
        {
            LookSensitivity = settings.LookSensitivity,
            InvertLook      = settings.InvertLook,
            MoveSpeed       = settings.MoveSpeed,
            Smoothing       = settings.Smoothing,
            LabelScale      = settings.LabelScale,
        };

        // Keeps loaded values inside the ranges the settings panel offers.
        public void Clamp()
        {
            LookSensitivity = Mathf.Clamp(LookSensitivity, 0.02f, 0.5f);
            MoveSpeed       = Mathf.Clamp(MoveSpeed, 2f, 40f);
            Smoothing       = Mathf.Clamp01(Smoothing);
            LabelScale      = Mathf.Clamp(LabelScale, 0.6f, 1.6f);
            if (!Enum.IsDefined(typeof(InspectorPage), InspectorPage)) InspectorPage = InspectorPage.Overview;
        }

        // Back to the defaults, keeping what is showing (labels, help, inspector, overlays).
        public void ResetView(SpectatorInputSettings settings)
        {
            SpectatorPreferences defaults = DefaultsFrom(settings);
            LookSensitivity = defaults.LookSensitivity;
            InvertLook      = defaults.InvertLook;
            MoveSpeed       = defaults.MoveSpeed;
            Smoothing       = defaults.Smoothing;
            LabelScale      = defaults.LabelScale;
        }
    }
}
