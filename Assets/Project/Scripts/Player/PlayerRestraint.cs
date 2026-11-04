using System;
using UnityEngine;
using CGD.Combat;
using CGD.Input;

namespace CGD.Player
{
    // Placeholder for being held in place: something calls Restrain (a trap, an enemy's grab, a
    // dev command) and the player can't move or dodge until they break free by tapping Interact,
    // or it lets go by itself. Interacting with things is off meanwhile. Does not yet stop
    // shooting or abilities, or deal damage while held — add those when a real grab exists.
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(Stunnable))]
    public class PlayerRestraint : MonoBehaviour
    {
        [Header("Defaults for Restrain()")]
        [SerializeField, Min(1)] private int _tapsRequired = 8;
        [Tooltip("Share of the bar lost per second once the player stops tapping")]
        [SerializeField, Min(0f)] private float _decayPerSecond = 0.4f;
        [Tooltip("Seconds until it lets go by itself (0 = holds until the player breaks free)")]
        [SerializeField, Min(0f)] private float _maxSeconds;

        private readonly RestraintState _state = new();
        private PlayerInputHandler _input;
        private Stunnable         _stunnable;
        private HealthManager     _health;

        public bool   IsRestrained => _state.IsActive;
        public float  Progress     => _state.Progress;
        // What the prompt says, e.g. "Break free".
        public string Label        { get; private set; } = string.Empty;
        // Counts every tap while restrained, for feedback that reacts to each one.
        public int    TapCount     { get; private set; }

        // Started, broke free or let go.
        public event Action Changed;

        private void Awake()
        {
            _input     = GetComponent<PlayerInputHandler>();
            _stunnable = GetComponent<Stunnable>();
            if (TryGetComponent(out _health)) _health.OnDeath += Release;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= Release;
        }

        // A value of 0 (or below) for taps, decay or seconds uses this component's default.
        public void Restrain(string label = "Break free", int tapsRequired = 0, float decayPerSecond = -1f, float maxSeconds = -1f)
        {
            if (_health != null && _health.IsDead) return;

            Label = label;
            _state.Begin(
                tapsRequired > 0 ? tapsRequired : _tapsRequired,
                decayPerSecond >= 0f ? decayPerSecond : _decayPerSecond,
                maxSeconds >= 0f ? maxSeconds : _maxSeconds);
            _stunnable.AddHold(this);
            Changed?.Invoke();
        }

        public void Release()
        {
            if (!_state.IsActive) return;

            _state.End();
            _stunnable.RemoveHold(this);
            Changed?.Invoke();
        }

        private void Update()
        {
            if (!_state.IsActive) return;

            bool tapped = _input.WasPressed(GameAction.Interact);
            if (tapped) TapCount++;

            if (_state.Tick(Time.deltaTime, tapped) == RestraintOutcome.None) return;

            _stunnable.RemoveHold(this);
            Changed?.Invoke();
        }
    }
}
