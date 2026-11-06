using UnityEngine;
using CGD.Combat;
using CGD.Flow;
using CGD.Player;

namespace CGD.Audio
{
    // Connects gameplay situations to MixStates: pausing, being low on health, being stunned.
    // Each state is optional; leave a slot empty and that situation leaves the mix alone.
    // Lives on the Player, which owns the health and movement it watches.
    public class GameplayMixStates : MonoBehaviour
    {
        [Header("States")]
        [SerializeField] private MixState _paused;
        [SerializeField] private MixState _lowHealth;
        [SerializeField] private MixState _stunned;

        [Header("Low health")]
        [Tooltip("Health fraction at or below which the low-health state applies")]
        [SerializeField, Range(0f, 1f)] private float _lowHealthThreshold = 0.3f;

        private HealthManager  _health;
        private PlayerMovement _movement;
        private GameFlow       _flow;

        private MixStateRequest _pausedRequest;
        private MixStateRequest _lowHealthRequest;
        private MixStateRequest _stunnedRequest;

        private void Awake()
        {
            TryGetComponent(out _health);
            TryGetComponent(out _movement);
        }

        private void OnEnable()
        {
            if (_health != null) _health.OnChanged += UpdateLowHealth;
        }

        // GameFlow sits on its own object and may wake after this one, so it is looked up here.
        private void Start()
        {
            _flow = GameFlow.Instance;
            if (_flow == null) return;

            _flow.StateChanged += OnFlowStateChanged;
            SetRequest(ref _pausedRequest, _paused, _flow.IsPaused);
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnChanged -= UpdateLowHealth;
            ReleaseAll();
        }

        private void OnDestroy()
        {
            if (_flow != null) _flow.StateChanged -= OnFlowStateChanged;
        }

        private void Update()
        {
            if (_movement != null) SetRequest(ref _stunnedRequest, _stunned, _movement.IsStunned);
        }

        private void OnFlowStateChanged(GameState previous, GameState next) =>
            SetRequest(ref _pausedRequest, _paused, next == GameState.Paused);

        private void UpdateLowHealth()
        {
            bool low = !_health.IsDead && _health.Health / _health.MaxHealth <= _lowHealthThreshold;
            SetRequest(ref _lowHealthRequest, _lowHealth, low);
        }

        private void ReleaseAll()
        {
            SetRequest(ref _pausedRequest,    _paused,    false);
            SetRequest(ref _lowHealthRequest, _lowHealth, false);
            SetRequest(ref _stunnedRequest,   _stunned,   false);
        }

        private static void SetRequest(ref MixStateRequest request, MixState state, bool wanted)
        {
            if (state == null) return;

            if (wanted && request == null)
            {
                request = AudioMixerController.Push(state);
            }
            else if (!wanted && request != null)
            {
                request.Release();
                request = null;
            }
        }
    }
}
