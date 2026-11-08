namespace CGD.DevTools
{
    // What each spectator action is called in the settings list and help panel.
    public static class SpectatorActionLabels
    {
        public static string Of(SpectatorAction action) => action switch
        {
            SpectatorAction.MoveForward   => "Move forward",
            SpectatorAction.MoveBack      => "Move back",
            SpectatorAction.MoveLeft      => "Move left",
            SpectatorAction.MoveRight     => "Move right",
            SpectatorAction.MoveUp        => "Move up",
            SpectatorAction.MoveDown      => "Move down",
            SpectatorAction.Fast          => "Faster (hold)",
            SpectatorAction.Look          => "Look around (hold)",
            SpectatorAction.ZoomIn        => "Zoom in",
            SpectatorAction.ZoomOut       => "Zoom out",
            SpectatorAction.NextAgent     => "Next agent",
            SpectatorAction.PreviousAgent => "Previous agent",
            SpectatorAction.Select        => "Pick agent (click)",
            SpectatorAction.WatchAgent1   => "Watch agent 1",
            SpectatorAction.WatchAgent2   => "Watch agent 2",
            SpectatorAction.WatchAgent3   => "Watch agent 3",
            SpectatorAction.WatchAgent4   => "Watch agent 4",
            SpectatorAction.Overview      => "Whole arena",
            SpectatorAction.FreeCamera    => "Free camera",
            SpectatorAction.Pause         => "Pause / resume",
            SpectatorAction.SlowDown      => "Slower",
            SpectatorAction.SpeedUp       => "Faster time",
            SpectatorAction.StepFrame     => "Step one frame",
            SpectatorAction.ToggleLabels  => "Show / hide labels",
            SpectatorAction.ToggleHelp    => "Show / hide help",
            SpectatorAction.Settings      => "Open settings",
            SpectatorAction.InspectorPage => "Inspector page",
            SpectatorAction.ToggleSenses  => "Senses overlay",
            SpectatorAction.ToggleCover   => "Cover overlay",
            SpectatorAction.TogglePlans   => "Paths & plans overlay",
            SpectatorAction.ToggleStats   => "Match stats",
            SpectatorAction.ResetStats    => "Reset match stats",
            SpectatorAction.Scenarios     => "Scenarios",
            SpectatorAction.RestartScenario => "Restart scenario",
            _                             => action.ToString(),
        };
    }
}
