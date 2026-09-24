namespace CGD.Flow
{
    // High-level phases of the game. Which moves between them are legal lives in
    // GameStateMachine.
    public enum GameState
    {
        Boot,            // first frame: deciding whether we start in a menu or a level
        MainMenu,
        Loading,         // a scene is loading asynchronously
        Playing,
        Paused,
        LevelTransition, // leaving a level (fade-out, results) before the next one loads
        GameOver,
        Victory,
    }
}
