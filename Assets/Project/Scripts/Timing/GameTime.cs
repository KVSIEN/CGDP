using UnityEngine;

namespace CGD.Timing
{
    // Runs the game's GameClock from real time and makes Unity follow it: Time.timeScale
    // always equals Clock.TimeScale, so pausing or slowing the clock also pauses or slows
    // physics, animation and every Update-driven system. Nothing else should write
    // Time.timeScale — request a scale or pause on the clock instead.
    //
    // One per game, persisting across scene loads. Place it (with settings) on the
    // GameFlow object, or leave it out: the first access creates one with default
    // settings (60 ticks per second).
    [DefaultExecutionOrder(-190)]
    public class GameTime : MonoBehaviour
    {
        [Tooltip("Optional — without it the clock runs at 60 ticks per second")]
        [SerializeField] private GameTimeSettings _settings;

        private static GameTime _instance;
        private static bool     _quitting;

        private float _baseFixedDeltaTime;

        public GameClock Clock { get; private set; }

        public static bool Exists => _instance != null;

        // Creates the instance on first use during play. Returns null while the application
        // is quitting, so teardown code must use Exists first.
        public static GameTime Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying && !_quitting)
                    new GameObject(nameof(GameTime)).AddComponent<GameTime>();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _quitting = false;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            _baseFixedDeltaTime = Time.fixedDeltaTime;
            Clock = _settings != null
                ? new GameClock(_settings.TickRate, _settings.MaxTicksPerFrame)
                : new GameClock();
            Clock.TimeScaleChanged += ApplyTimeScale;
        }

        private void Update() => Clock.Advance(Time.unscaledDeltaTime);

        private void OnApplicationQuit() => _quitting = true;

        private void OnDestroy()
        {
            if (_instance != this) return;

            _instance = null;
            Time.timeScale      = 1f;
            Time.fixedDeltaTime = _baseFixedDeltaTime;
        }

        private void ApplyTimeScale(float scale)
        {
            Time.timeScale = scale;

            // At 0 physics doesn't step at all, so the step length doesn't matter.
            bool scalePhysics = _settings == null || _settings.ScalePhysicsStep;
            if (scalePhysics && scale > 0f)
                Time.fixedDeltaTime = _baseFixedDeltaTime * Mathf.Min(scale, 1f);
        }
    }
}
