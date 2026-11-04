using System;

namespace CGD.Input
{
    // PrimaryPath/SecondaryPath are Input System control paths (e.g. "<Keyboard>/g",
    // "<Mouse>/leftButton", "<Gamepad>/buttonSouth") — the same format
    // InputAction.AddBinding expects and InputActionRebindingExtensions.PerformInteractiveRebinding
    // resolves to directly, so no hand-rolled enum-to-path translation is needed anywhere.
    [Serializable]
    public struct ActionBinding
    {
        public GameAction Action;
        public string PrimaryPath;
        public string SecondaryPath;
        public InputActionMode Mode;
    }
}
