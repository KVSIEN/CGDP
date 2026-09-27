using System;

namespace CGD.Stealth
{
    // The stealth state machine for one character, without Unity: concealment comes in
    // (from cover or a cloak), noise and attacks reveal for a moment, and being seen is
    // remembered briefly for the HUD. Stealthable feeds it every frame.
    public class StealthStatus
    {
        private float _cloakRemaining;
        private bool  _cloakBreaksOnNoise;
        private float _revealRemaining;
        private float _spottedRemaining;

        public StealthState State     { get; private set; } = StealthState.Visible;
        public bool IsCloaked         => _cloakRemaining > 0f;
        public float CloakRemaining   => Math.Max(0f, _cloakRemaining);
        public bool IsSpotted         => _spottedRemaining > 0f;
        public bool IsHidden          => State == StealthState.Hidden;

        // (previous, next)
        public event Action<StealthState, StealthState> Changed;
        public event Action<bool> SpottedChanged;

        public void Cloak(float seconds, bool breaksOnNoise)
        {
            _cloakRemaining     = Math.Max(_cloakRemaining, seconds);
            _cloakBreaksOnNoise = breaksOnNoise;
        }

        public void EndCloak() => _cloakRemaining = 0f;

        // Attacking or making noise: a breakable cloak ends, and cover stops hiding you
        // for `seconds`.
        public void OnNoise(float seconds)
        {
            if (_cloakBreaksOnNoise) _cloakRemaining = 0f;
            _revealRemaining = Math.Max(_revealRemaining, seconds);
        }

        public void OnSpotted(float memory)
        {
            bool was = IsSpotted;
            _spottedRemaining = Math.Max(_spottedRemaining, memory);
            if (!was) SpottedChanged?.Invoke(true);
        }

        public void Tick(float deltaTime, bool inCover)
        {
            _cloakRemaining  -= deltaTime;
            _revealRemaining -= deltaTime;

            if (_spottedRemaining > 0f)
            {
                _spottedRemaining -= deltaTime;
                if (_spottedRemaining <= 0f) SpottedChanged?.Invoke(false);
            }

            bool concealed = inCover || IsCloaked;
            StealthState next = !concealed           ? StealthState.Visible
                              : _revealRemaining > 0f ? StealthState.Revealed
                              :                        StealthState.Hidden;
            if (next == State) return;

            StealthState previous = State;
            State = next;
            Changed?.Invoke(previous, next);
        }

        public void Reset()
        {
            _cloakRemaining = _revealRemaining = _spottedRemaining = 0f;
            if (State == StealthState.Visible) return;

            StealthState previous = State;
            State = StealthState.Visible;
            Changed?.Invoke(previous, State);
        }
    }
}
