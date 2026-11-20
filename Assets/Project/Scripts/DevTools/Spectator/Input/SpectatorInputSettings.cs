using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Input;

namespace CGD.DevTools
{
    // The spectator camera's controls and its default feel. A binding table of its own rather
    // than GameAction entries: spectator mode only runs with the Player switched off, and its
    // natural keys (WASD, Q/E, Tab, 1–4…) overlap the player's on purpose, so in the player's
    // table every one would show as a conflict. A BindingTable like InputBindingSettings:
    // rebindable in the spectator's own settings panel, saved to PlayerPrefs under its own key.
    [CreateAssetMenu(fileName = "SpectatorInputSettings", menuName = "CGD/DevTools/Spectator Input Settings")]
    public class SpectatorInputSettings : BindingTable<SpectatorAction>, IRestoredAfterPlayMode
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

        // Not "spectator_bindings": that key holds an older save format.
        protected override string SaveKey => "spectator_controls";

        // Escape always opens the settings; that one row isn't rebindable.
        public static bool IsFixed(SpectatorAction action) => action == SpectatorAction.Settings;

        public override bool IsRebindable(SpectatorAction action) => !IsFixed(action);

        public override string LabelOf(SpectatorAction action) => SpectatorActionLabels.Of(action);

        public override int Count => Bindings.Count;

        public override SpectatorAction ActionAt(int index) => Bindings[index].Action;

        public override string PathAt(int index, bool primary) =>
            primary ? Bindings[index].PrimaryPath : Bindings[index].SecondaryPath;

        public override void SetPath(int index, bool primary, string path)
        {
            SpectatorBinding binding = Bindings[index];
            if (primary) binding.PrimaryPath   = path;
            else         binding.SecondaryPath = path;
            Bindings[index] = binding;
        }

        public bool TryGet(SpectatorAction action, out SpectatorBinding binding)
        {
            int index = IndexOf(action);
            binding = index >= 0 ? Bindings[index] : default;
            return index >= 0;
        }

        // Adds the default for any action the list lacks (an action added after the asset was
        // made), so every action can always be read.
        public void EnsureComplete()
        {
            foreach (SpectatorBinding binding in DefaultBindings())
                if (IndexOf(binding.Action) < 0) Bindings.Add(binding);
        }

        [ContextMenu("Reset Bindings to Defaults")]
        public override void ResetToDefaults() => Bindings = DefaultBindings();

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
            Bind(SpectatorAction.WatchAgent5,   "<Keyboard>/5"),
            Bind(SpectatorAction.WatchAgent6,   "<Keyboard>/6"),
            Bind(SpectatorAction.Overview,     "<Keyboard>/0",          "<Gamepad>/buttonNorth"),
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
            Bind(SpectatorAction.ToggleStats,   "<Keyboard>/m"),
            Bind(SpectatorAction.ResetStats,    "<Keyboard>/backspace"),
            Bind(SpectatorAction.Scenarios,     "<Keyboard>/b"),
            Bind(SpectatorAction.RestartScenario, "<Keyboard>/r"),
            Bind(SpectatorAction.Squads,        "<Keyboard>/g"),
        };

        private static SpectatorBinding Bind(SpectatorAction action, string primary, string secondary = "") =>
            new() { Action = action, PrimaryPath = primary, SecondaryPath = secondary };

        private void Reset() => ResetToDefaults();
    }
}
