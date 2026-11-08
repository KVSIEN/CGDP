using System;

namespace CGD.DevTools
{
    // One spectator action's controls: Input System paths ("<Keyboard>/w", "<Mouse>/rightButton",
    // "<Gamepad>/rightShoulder"); empty = unbound. Either slot triggers the action.
    [Serializable]
    public struct SpectatorBinding
    {
        public SpectatorAction Action;
        public string PrimaryPath;
        public string SecondaryPath;
    }
}
