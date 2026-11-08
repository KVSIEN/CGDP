using CGD.Timing;

namespace CGD.DevTools
{
    // Pause, slow motion, fast forward and frame stepping for watching a fight, through the
    // game clock (GameClock's pause owners and scale requests), so it combines with anything
    // else that slows or pauses time. Release() hands time back exactly as it found it.
    internal sealed class SpectatorTime
    {
        private static readonly float[] Speeds = { 0.1f, 0.25f, 0.5f, 1f, 2f, 4f };
        private const int NormalSpeed = 3;

        private int  _speed = NormalSpeed;
        private TimeScaleRequest _request;
        private bool _paused;
        private bool _pauseAgain;   // stepping: paused again once the frame has run

        public bool  IsPaused => _paused;
        public float Speed    => Speeds[_speed];

        public void TogglePause()
        {
            _paused     = !_paused;
            _pauseAgain = false;
            if (_paused) Clock.Pause(this);
            else         Clock.Resume(this);
        }

        public void Faster() => SetSpeed(_speed + 1);
        public void Slower() => SetSpeed(_speed - 1);

        // While paused: lets exactly one frame run.
        public void StepFrame()
        {
            if (!_paused) return;
            Clock.Resume(this);
            _pauseAgain = true;
        }

        // Once a frame, before the step's frame would run a second time.
        public void Tick()
        {
            if (!_pauseAgain) return;
            _pauseAgain = false;
            Clock.Pause(this);
        }

        public void Release()
        {
            _request?.Release();
            _request    = null;
            _speed      = NormalSpeed;
            _paused     = false;
            _pauseAgain = false;
            if (GameTime.Exists) GameTime.Instance.Clock.Resume(this);
        }

        private static GameClock Clock => GameTime.Instance.Clock;

        private void SetSpeed(int index)
        {
            int clamped = System.Math.Clamp(index, 0, Speeds.Length - 1);
            if (clamped == _speed) return;

            _speed = clamped;
            _request?.Release();
            _request = _speed == NormalSpeed ? null : Clock.RequestScale(Speeds[_speed]);
        }
    }
}
