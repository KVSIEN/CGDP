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
        ZoomIn,         // overview: lower; following: closer; free: slower
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
    }
}
