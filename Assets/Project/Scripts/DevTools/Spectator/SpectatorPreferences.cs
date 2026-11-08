using System;
using UnityEngine;

namespace CGD.DevTools
{
    // How this viewer likes the spectator camera: look feel, speed, smoothing, labels and
    // whether the help is showing. Starts from SpectatorInputSettings' defaults and is kept
    // in PlayerPrefs (SpectatorSave), so it survives sessions.
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
        }

        // Back to the defaults, keeping whether labels and help are showing.
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
