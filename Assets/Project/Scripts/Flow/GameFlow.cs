using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CGD.Core;
using CGD.Timing;

namespace CGD.Flow
{
    // Owns the game's high-level state (menu, playing, paused, loading, game over...) and
    // everything that has to change with it: pausing game time, world audio, the cursor
    // and scene loading. Gameplay and UI ask it to change state; they never pause the
    // clock or touch SceneManager themselves. Time itself belongs to GameTime.
    //
    // Persists across scene loads. Every scene may contain one so it can be played
    // straight from the editor — the first to wake up wins and later copies remove
    // themselves. Because of that, scene objects reach it through Instance (see
    // GameFlowCommands for UI buttons) and must tolerate it being absent.
    [DefaultExecutionOrder(-200)]
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private GameFlowSettings _settings;

        public static GameFlow Instance { get; private set; }

        private readonly GameStateMachine _machine = new();
        private Coroutine _loadRoutine;

        public GameState State        => _machine.Current;
        public bool      IsPaused     => State == GameState.Paused;
        public bool      IsLoading    => _loadRoutine != null;
        // 0..1 while Loading.
        public float     LoadProgress { get; private set; }

        // (previous, next)
        public event Action<GameState, GameState> StateChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Without an asset the defaults apply: no menu scene, short transitions.
            if (_settings == null) _settings = ScriptableObject.CreateInstance<GameFlowSettings>();

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            _machine.Changed += OnStateChanged;
        }

        private void Start()
        {
            if (State == GameState.Boot)
                _machine.TryEnter(IsMenuScene(SceneManager.GetActiveScene().name) ? GameState.MainMenu : GameState.Playing);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            Instance = null;
            if (GameTime.Exists) GameTime.Instance.Clock.Resume(this);
            AudioListener.pause = false;
        }

        public bool Pause()  => _machine.TryEnter(GameState.Paused);
        public bool Resume() => IsPaused && _machine.TryEnter(GameState.Playing);

        public void TogglePause()
        {
            if (IsPaused) Resume();
            else          Pause();
        }

        public bool GameOver() => _machine.TryEnter(GameState.GameOver);
        public bool Victory()  => _machine.TryEnter(GameState.Victory);

        public bool StartGame()    => LoadScene(_settings.FirstLevelScene);
        public bool LoadMainMenu() => LoadScene(_settings.MainMenuScene);
        public bool RestartLevel() => LoadScene(SceneManager.GetActiveScene().name);

        // Leaves the current scene (through LevelTransition when coming from gameplay),
        // loads the named one asynchronously, then enters MainMenu or Playing. Refused
        // while another load is running or from a state that can't load.
        public bool LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || IsLoading) return false;
            if (!_machine.CanEnter(GameState.LevelTransition) && !_machine.CanEnter(GameState.Loading)) return false;

            _loadRoutine = StartCoroutine(LoadRoutine(sceneName));
            return true;
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            if (_machine.TryEnter(GameState.LevelTransition))
                yield return new WaitForSecondsRealtime(_settings.TransitionDuration);

            _machine.TryEnter(GameState.Loading);
            LoadProgress = 0f;
            float startTime = Time.unscaledTime;

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            if (load == null)
            {
                // Not in the build's scene list; Unity has already logged why.
                FinishLoading(SceneManager.GetActiveScene().name);
                yield break;
            }

            // Hold activation so the loading screen can honour its minimum time.
            load.allowSceneActivation = false;
            while (load.progress < 0.9f || Time.unscaledTime - startTime < _settings.MinimumLoadingTime)
            {
                LoadProgress = Mathf.Clamp01(load.progress / 0.9f);
                yield return null;
            }

            LoadProgress = 1f;
            load.allowSceneActivation = true;
            yield return load;

            FinishLoading(sceneName);
        }

        private void FinishLoading(string sceneName)
        {
            _loadRoutine = null;
            _machine.TryEnter(IsMenuScene(sceneName) ? GameState.MainMenu : GameState.Playing);
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            bool paused = next == GameState.Paused;
            if (paused) GameTime.Instance.Clock.Pause(this);
            else        GameTime.Instance.Clock.Resume(this);
            AudioListener.pause = paused && _settings.PauseAudio;
            CursorLock.Set(next == GameState.Playing);

            StateChanged?.Invoke(previous, next);
        }

        private bool IsMenuScene(string sceneName) =>
            !string.IsNullOrEmpty(_settings.MainMenuScene) && sceneName == _settings.MainMenuScene;
    }
}
