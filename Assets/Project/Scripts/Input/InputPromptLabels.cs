using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace CGD.Input
{
    // Short on-screen names for the controls an action is bound to ("E", "LMB", "Shift"),
    // for key hints in the HUD. The settings menu lists full names instead.
    public static class InputPromptLabels
    {
        public const string Unbound = "—";

        private static readonly Dictionary<string, string> Abbreviations = new()
        {
            { "Left Button",   "LMB" },
            { "Right Button",  "RMB" },
            { "Middle Button", "MMB" },
            { "Left Shift",    "Shift" },
            { "Right Shift",   "Shift" },
            { "Left Ctrl",     "Ctrl" },
            { "Right Ctrl",    "Ctrl" },
            { "Left Alt",      "Alt" },
            { "Right Alt",     "Alt" },
            { "Escape",        "Esc" },
            { "Backquote",     "`" },
        };

        // `path` is an Input System control path such as "<Keyboard>/e".
        public static string Short(string path)
        {
            if (string.IsNullOrEmpty(path)) return Unbound;

            string readable = InputControlPath.ToHumanReadableString(
                path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            return Abbreviate(readable);
        }

        public static string Abbreviate(string readable)
        {
            if (string.IsNullOrEmpty(readable)) return Unbound;
            if (Abbreviations.TryGetValue(readable, out string shortName)) return shortName;
            if (readable.StartsWith("Scroll"))
                return readable.EndsWith("Up") ? "Wheel Up" : readable.EndsWith("Down") ? "Wheel Down" : "Wheel";
            return readable;
        }
    }
}
