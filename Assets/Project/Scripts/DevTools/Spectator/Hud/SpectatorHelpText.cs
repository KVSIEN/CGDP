namespace CGD.DevTools
{
    // The controls panel and the hint line, written from the live bindings so they follow
    // any rebinding (rebuilt whenever SpectatorInputSettings.Changed fires).
    internal static class SpectatorHelpText
    {
        public static string Controls(SpectatorInputSettings settings)
        {
            string P(SpectatorAction action) => settings.PromptFor(action);

            string move  = $"{P(SpectatorAction.MoveForward)} {P(SpectatorAction.MoveLeft)} {P(SpectatorAction.MoveBack)} {P(SpectatorAction.MoveRight)}";
            string watch = $"{P(SpectatorAction.WatchAgent1)} {P(SpectatorAction.WatchAgent2)} {P(SpectatorAction.WatchAgent3)} {P(SpectatorAction.WatchAgent4)}";

            return
                "<b>SPECTATOR CONTROLS</b>\n" +
                Section("Camera") +
                Line("Move / pan", $"{move}   ({P(SpectatorAction.MoveDown)} {P(SpectatorAction.MoveUp)} down / up)") +
                Line("Faster", $"hold {P(SpectatorAction.Fast)}") +
                Line("Look / orbit", $"hold {P(SpectatorAction.Look)}") +
                Line("Zoom", $"{P(SpectatorAction.ZoomIn)} / {P(SpectatorAction.ZoomOut)}   (free: fly speed)") +
                Section("Watching") +
                Line("Watch an agent", $"{watch}   or click it") +
                Line("Next / previous", $"{P(SpectatorAction.NextAgent)}   ({P(SpectatorAction.Fast)}+{P(SpectatorAction.NextAgent)} back)") +
                Line("Whole arena", $"{P(SpectatorAction.Overview)}   (keeps inspecting)") +
                Line("Free camera", $"{P(SpectatorAction.FreeCamera)}   or just steer away") +
                Line("Stop inspecting", "click empty ground") +
                Line("Inspector page", P(SpectatorAction.InspectorPage)) +
                Section("Overlays") +
                Line("Senses", P(SpectatorAction.ToggleSenses)) +
                Line("Cover", P(SpectatorAction.ToggleCover)) +
                Line("Paths & plans", P(SpectatorAction.TogglePlans)) +
                Section("Match") +
                Line("Match stats", P(SpectatorAction.ToggleStats)) +
                Line("Reset stats", P(SpectatorAction.ResetStats)) +
                Line("Scenarios", P(SpectatorAction.Scenarios)) +
                Line("Restart scenario", P(SpectatorAction.RestartScenario)) +
                Section("Time") +
                Line("Pause / step", $"{P(SpectatorAction.Pause)}   /   {P(SpectatorAction.StepFrame)}") +
                Line("Slower / faster", $"{P(SpectatorAction.SlowDown)}   /   {P(SpectatorAction.SpeedUp)}") +
                Section("Display") +
                Line("Labels", P(SpectatorAction.ToggleLabels)) +
                Line("Hide this help", P(SpectatorAction.ToggleHelp)) +
                Line("Settings, rebinding", P(SpectatorAction.Settings));
        }

        public static string Hint(SpectatorInputSettings settings)
        {
            string P(SpectatorAction action) => settings.PromptFor(action);
            return $"<b>{P(SpectatorAction.ToggleHelp)}</b> controls     <b>{P(SpectatorAction.Settings)}</b> settings     " +
                   $"<b>{P(SpectatorAction.WatchAgent1)}–{P(SpectatorAction.WatchAgent4)}</b> watch an agent     " +
                   $"<b>{P(SpectatorAction.InspectorPage)}</b> inspector page     " +
                   $"<b>{P(SpectatorAction.ToggleSenses)} {P(SpectatorAction.ToggleCover)} {P(SpectatorAction.TogglePlans)}</b> overlays     " +
                   $"<b>{P(SpectatorAction.Scenarios)}</b> scenarios";
        }

        private static string Section(string title) => $"<color=#FFD24A>{title}</color>\n";

        private static string Line(string label, string keys) => $"{label}<pos=44%><b>{keys}</b>\n";
    }
}
