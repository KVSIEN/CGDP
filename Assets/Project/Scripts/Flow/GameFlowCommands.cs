using UnityEngine;

namespace CGD.Flow
{
    // Inspector-friendly entry points into GameFlow for UI Buttons and other UnityEvents.
    // GameFlow persists across scenes, so a scene's buttons can't reference it directly —
    // they call this component, which forwards to GameFlow.Instance (and does nothing
    // when there is none).
    public class GameFlowCommands : MonoBehaviour
    {
        public void Pause()        => GameFlow.Instance?.Pause();
        public void Resume()       => GameFlow.Instance?.Resume();
        public void TogglePause()  => GameFlow.Instance?.TogglePause();
        public void StartGame()    => GameFlow.Instance?.StartGame();
        public void RestartLevel() => GameFlow.Instance?.RestartLevel();
        public void LoadMainMenu() => GameFlow.Instance?.LoadMainMenu();
        public void LoadScene(string sceneName) => GameFlow.Instance?.LoadScene(sceneName);
        public void GameOver()     => GameFlow.Instance?.GameOver();
        public void Victory()      => GameFlow.Instance?.Victory();
        public void Quit()         => GameFlow.Instance?.Quit();
    }
}
