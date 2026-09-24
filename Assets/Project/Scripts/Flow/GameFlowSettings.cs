using UnityEngine;

namespace CGD.Flow
{
    // Scene names and timings for GameFlow. One shared asset, referenced by the GameFlow
    // in every scene that has one (see SETUP.md). Scenes must be in the Build Profile's
    // scene list to be loadable by name.
    [CreateAssetMenu(fileName = "GameFlowSettings", menuName = "CGD/Flow/Game Flow Settings")]
    public class GameFlowSettings : ScriptableObject
    {
        [Header("Scenes")]
        [Tooltip("Scene loaded by 'Main Menu' commands. Empty = there is no menu scene yet.")]
        [SerializeField] private string _mainMenuScene;
        [Tooltip("Scene the main menu's 'Play' starts")]
        [SerializeField] private string _firstLevelScene = "Sandbox";

        [Header("Timing (unscaled seconds)")]
        [Tooltip("How long LevelTransition lasts before loading starts — time for a fade-out or results panel")]
        [SerializeField, Min(0f)] private float _transitionDuration = 0.5f;
        [Tooltip("Loading screens stay up at least this long so they don't flash")]
        [SerializeField, Min(0f)] private float _minimumLoadingTime = 0.5f;

        [Header("Pause")]
        [Tooltip("Also pause world audio while the game is paused")]
        [SerializeField] private bool _pauseAudio = true;

        public string MainMenuScene      => _mainMenuScene;
        public string FirstLevelScene    => _firstLevelScene;
        public float  TransitionDuration => _transitionDuration;
        public float  MinimumLoadingTime => _minimumLoadingTime;
        public bool   PauseAudio         => _pauseAudio;
    }
}
