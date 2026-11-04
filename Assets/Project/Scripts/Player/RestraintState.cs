using CGD.Interaction;

namespace CGD.Player
{
    // The rules of being held in place (grabbed, webbed, trapped) until the player breaks free
    // by tapping, or it lets go by itself. Plain logic; PlayerRestraint feeds it input and time.
    public sealed class RestraintState
    {
        private readonly EffortMeter _effort = new();

        private int   _tapsRequired;
        private float _decayPerSecond;
        private float _secondsLeft;
        private bool  _hasTimeLimit;

        public bool  IsActive { get; private set; }

        // 0..1 toward breaking free.
        public float Progress => _effort.Progress;

        // `maxSeconds` of 0 means it holds until the player breaks free.
        public void Begin(int tapsRequired, float decayPerSecond, float maxSeconds)
        {
            _effort.Reset();
            _tapsRequired   = tapsRequired;
            _decayPerSecond = decayPerSecond;
            _hasTimeLimit   = maxSeconds > 0f;
            _secondsLeft    = maxSeconds;
            IsActive        = true;
        }

        public void End()
        {
            _effort.Reset();
            IsActive = false;
        }

        // One frame. `tapped` is whether the player pressed the key this frame. The outcome says
        // when the restraint ended on this frame.
        public RestraintOutcome Tick(float deltaTime, bool tapped)
        {
            if (!IsActive) return RestraintOutcome.None;

            _effort.Drain(_decayPerSecond, deltaTime);
            if (tapped && _effort.Tap(_tapsRequired))
            {
                End();
                return RestraintOutcome.BrokeFree;
            }

            if (!_hasTimeLimit) return RestraintOutcome.None;

            _secondsLeft -= deltaTime;
            if (_secondsLeft > 0f) return RestraintOutcome.None;

            End();
            return RestraintOutcome.TimedOut;
        }
    }
}
