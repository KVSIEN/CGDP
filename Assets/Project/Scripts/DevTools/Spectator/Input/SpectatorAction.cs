namespace CGD.DevTools
{
    // Everything the spectator camera can be told to do (SpectatorInputSettings binds each to
    // a key, mouse or gamepad button). Saved by name, but append new actions at the end anyway.
    public enum SpectatorAction
    {
        MoveForward,
        MoveBack,
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        Fast,           // held: move, pan and fly faster; with Next Agent, go back one
        Look,           // held: the mouse turns the camera (and orbits a followed agent)
        ZoomIn,         // overview: lower; following: closer; free: forward / back along the view
        ZoomOut,
        NextAgent,
        PreviousAgent,
        Select,         // click an agent (or its label) to follow it
        WatchAgent1,
        WatchAgent2,
        WatchAgent3,
        WatchAgent4,
        Overview,       // back to the whole arena
        FreeCamera,     // fly freely from where the camera is
        Pause,
        SlowDown,
        SpeedUp,
        StepFrame,      // while paused: one frame on
        ToggleLabels,
        ToggleHelp,
        Settings,       // always Escape too, so the settings can't be locked away
        InspectorPage,  // the inspected agent's panel: overview → blackboard → decisions → hidden
        ToggleSenses,   // world overlay: sight cones, hearing, who it targets and why
        ToggleCover,    // world overlay: cover spots, who holds them, what it knows
        TogglePlans,    // world overlay: paths and the latest cover / flank / vanish searches
        ToggleStats,    // the match stats panel
        ResetStats,     // start the match stats over
        Scenarios,      // the scenario list (AIScenario presets)
        RestartScenario, // load the last scenario again, as it started
        WatchAgent5,    // appended later, so not contiguous with WatchAgent1–4
        WatchAgent6,
        Squads,         // the squad sizes list: how many of each faction are in the fight
    }
}
