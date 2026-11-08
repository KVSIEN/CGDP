using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Input;

namespace CGD.DevTools
{
    // The spectator camera's controls and its default feel. A binding table of its own rather
    // than GameAction entries: spectator mode only runs with the Player switched off, and its
    // natural keys (WASD, Q/E, Tab, 1–4…) overlap the player's on purpose, so in the player's
    // table every one would show as a conflict. Same pattern as InputBindingSettings:
    // rebindable in the spectator's own settings panel, saved to PlayerPrefs (SpectatorSave).
    [CreateAssetMenu(fileName = "SpectatorInputSettings", menuName = "CGD/DevTools/Spectator Input Settings")]
    public class SpectatorInputSettings : ScriptableObject
    {
        public List<SpectatorBinding> Bindings = new();

        [Header("Defaults (each viewer can change these in the spectator settings)")]
        [Tooltip("Degrees the camera turns per pixel of mouse movement")]
        [Range(0.02f, 0.5f)] public float LookSensitivity = 0.15f;
        public bool InvertLook;
        [Tooltip("Metres per second when flying or panning (×3 while Faster is held)")]
        [Range(2f, 40f)] public float MoveSpeed = 12f;
        [Tooltip("0 = the camera snaps to where it is steered, 1 = it glides there")]
        [Range(0f, 1f)] public float Smoothing = 0.35f;
        [Tooltip("Size of the labels over agents")]
        [Range(0.6f, 1.6f)] public float LabelScale = 1f;

        [Header("Gamepad")]
        [Tooltip("Degrees per second the right stick turns the camera at full tilt")]
        [Min(10f)] public float StickLookSpeed = 140f;

        // Raised after bindings were loaded, rebound or reset, so hints can redraw.
        public event Action Changed;

        public void NotifyChanged() => Changed?.Invoke();

        // Escape always opens the settings; that one row isn't rebindable.
        public static bool IsFixed(SpectatorAction action) => action == SpectatorAction.Settings;

        // Short name of the control for an action: its primary binding, else its secondary.
        public string PromptFor(SpectatorAction action)
        {
            if (!TryGet(action, out SpectatorBinding binding)) return InputPromptLabels.Unbound;
            string path = string.IsNullOrEmpty(binding.PrimaryPath) ? binding.SecondaryPath : binding.PrimaryPath;
            return InputPromptLabels.Short(path);
        }

        public bool TryGet(SpectatorAction action, out SpectatorBinding binding)
        {
            int index = IndexOf(action);
            binding = index >= 0 ? Bindings[index] : default;
            return index >= 0;
        }

        public int IndexOf(SpectatorAction action)
        {
            for (int i = 0; i < Bindings.Count; i++)
                if (Bindings[i].Action == action) return i;
            return -1;
        }

        // Adds the default for any action the list lacks (an action added after the asset was
        // made), so every action can always be read.
        public void EnsureComplete()
        {
            foreach (SpectatorBinding binding in DefaultBindings())
                if (IndexOf(binding.Action) < 0) Bindings.Add(binding);
        }

        [ContextMenu("Reset Bindings to Defaults")]
        public void ResetBindings() => Bindings = DefaultBindings();

        // Keyboard and mouse first; gamepad in the secondary slot where it has an obvious button.
        // The sticks and triggers (move, look, up/down) always work on top of these.
        public static List<SpectatorBinding> DefaultBindings() => new()
        {
            Bind(SpectatorAction.MoveForward,   "<Keyboard>/w"),
            Bind(SpectatorAction.MoveBack,      "<Keyboard>/s"),
            Bind(SpectatorAction.MoveLeft,      "<Keyboard>/a"),
            Bind(SpectatorAction.MoveRight,     "<Keyboard>/d"),
            Bind(SpectatorAction.MoveUp,        "<Keyboard>/e"),
            Bind(SpectatorAction.MoveDown,      "<Keyboard>/q"),
            Bind(SpectatorAction.Fast,          "<Keyboard>/leftShift",  "<Gamepad>/leftStickPress"),
            Bind(SpectatorAction.Look,          "<Mouse>/rightButton"),
            Bind(SpectatorAction.ZoomIn,        "<Mouse>/scroll/up",     "<Gamepad>/dpad/up"),
            Bind(SpectatorAction.ZoomOut,       "<Mouse>/scroll/down",   "<Gamepad>/dpad/down"),
            Bind(SpectatorAction.NextAgent,     "<Keyboard>/tab",        "<Gamepad>/rightShoulder"),
            Bind(SpectatorAction.PreviousAgent, "",                      "<Gamepad>/leftShoulder"),
            Bind(SpectatorAction.Select,        "<Mouse>/leftButton"),
            Bind(SpectatorAction.WatchAgent1,   "<Keyboard>/1"),
            Bind(SpectatorAction.WatchAgent2,   "<Keyboard>/2"),
            Bind(SpectatorAction.WatchAgent3,   "<Keyboard>/3"),
            Bind(SpectatorAction.WatchAgent4,   "<Keyboard>/4"),
            Bind(SpectatorAction.Overview,      "<Keyboard>/0",          "<Gamepad>/buttonNorth"),
            Bind(SpectatorAction.FreeCamera,    "<Keyboard>/f",          "<Gamepad>/buttonWest"),
            Bind(SpectatorAction.Pause,         "<Keyboard>/p",          "<Gamepad>/start"),
            Bind(SpectatorAction.SlowDown,      "<Keyboard>/minus",      "<Gamepad>/dpad/left"),
            Bind(SpectatorAction.SpeedUp,       "<Keyboard>/equals",     "<Gamepad>/dpad/right"),
            Bind(SpectatorAction.StepFrame,     "<Keyboard>/period",     "<Gamepad>/buttonEast"),
            Bind(SpectatorAction.ToggleLabels,  "<Keyboard>/l",          "<Gamepad>/select"),
            Bind(SpectatorAction.ToggleHelp,    "<Keyboard>/h",          "<Keyboard>/f1"),
            Bind(SpectatorAction.Settings,      "<Keyboard>/escape"),
            Bind(SpectatorAction.InspectorPage, "<Keyboard>/i",          "<Gamepad>/rightStickPress"),
            Bind(SpectatorAction.ToggleSenses,  "<Keyboard>/v"),
            Bind(SpectatorAction.ToggleCover,   "<Keyboard>/c"),
            Bind(SpectatorAction.TogglePlans,   "<Keyboard>/n"),
        };

        private static SpectatorBinding Bind(SpectatorAction action, string primary, string secondary = "") =>
            new() { Action = action, PrimaryPath = primary, SecondaryPath = secondary };

        private void Reset() => ResetBindings();
    }
}
