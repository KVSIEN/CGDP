using System;
using System.Collections.Generic;

namespace CGD.Timing
{
    // Game time as a count of fixed ticks (60 per game-second by default), independent
    // of frame rate. Plain C#: GameTime feeds it real time each frame and mirrors its
    // time scale onto Unity's.
    //
    // Pausing and time scaling live here so they compose: pause owners are counted (the
    // pause menu and a cutscene can both pause, and time resumes only when both let go),
    // and scale requests multiply (slow motion during hit stop is slower still).
    public class GameClock
    {
        public const int DefaultTickRate = 60;

        private readonly List<Registration>     _tickables     = new();
        private readonly List<TimeScaleRequest> _scaleRequests = new();
        private readonly HashSet<object>        _pauseOwners   = new();
        private readonly List<ScheduledCall>    _scheduled     = new();

        private ITickable[] _tickOrder = Array.Empty<ITickable>();
        private bool        _tickOrderDirty;
        private double      _accumulator;
        private float       _timeScale = 1f;
        private int         _registrations;

        public GameClock(int tickRate = DefaultTickRate, int maxTicksPerAdvance = 8)
        {
            TickRate           = Math.Max(1, tickRate);
            TickDuration       = 1f / TickRate;
            MaxTicksPerAdvance = Math.Max(1, maxTicksPerAdvance);
        }

        public int   TickRate           { get; }
        public float TickDuration       { get; }
        public int   MaxTicksPerAdvance { get; }

        // Ticks run since the clock started.
        public long Tick { get; private set; }

        // Game seconds elapsed: Tick × TickDuration. Stops while paused, slows with scale.
        public double Time => Tick * (double)TickDuration;

        // How far between the last tick and the next one we are, 0..1 — for smoothing
        // visuals of tick-driven objects between ticks.
        public float Alpha => (float)(_accumulator / TickDuration);

        public bool IsPaused => _pauseOwners.Count > 0;

        // 0 while paused, otherwise the product of every active scale request.
        public float TimeScale => _timeScale;

        public event Action<float> TimeScaleChanged;

        // --- Pause and scale -------------------------------------------------------

        public void Pause(object owner)
        {
            if (_pauseOwners.Add(owner)) RefreshTimeScale();
        }

        public void Resume(object owner)
        {
            if (_pauseOwners.Remove(owner)) RefreshTimeScale();
        }

        public bool IsPausedBy(object owner) => _pauseOwners.Contains(owner);

        // realDuration > 0 releases the request after that many unscaled seconds.
        public TimeScaleRequest RequestScale(float scale, float realDuration = 0f)
        {
            var request = new TimeScaleRequest(this, Math.Max(0f, scale), Math.Max(0f, realDuration));
            _scaleRequests.Add(request);
            RefreshTimeScale();
            return request;
        }

        internal void Release(TimeScaleRequest request)
        {
            if (!request.IsActive) return;

            request.IsActive = false;
            _scaleRequests.Remove(request);
            RefreshTimeScale();
        }

        // --- Tickables and scheduling ---------------------------------------------

        // Lower order ticks first. Safe to call from inside a tick; takes effect next tick.
        public void Register(ITickable tickable, int order = 0)
        {
            if (_tickables.Exists(r => r.Tickable == tickable)) return;

            _tickables.Add(new Registration(tickable, order, _registrations++));
            _tickOrderDirty = true;
        }

        public void Unregister(ITickable tickable)
        {
            if (_tickables.RemoveAll(r => r.Tickable == tickable) > 0)
                _tickOrderDirty = true;
        }

        // Runs callback after delay game-seconds, rounded up to whole ticks (at least one).
        public ScheduledCall Schedule(float delaySeconds, Action callback) =>
            ScheduleTicks(SecondsToTicks(delaySeconds), callback);

        public ScheduledCall ScheduleTicks(int ticks, Action callback)
        {
            var call = new ScheduledCall(Tick + Math.Max(1, ticks), callback);
            _scheduled.Add(call);
            return call;
        }

        public int SecondsToTicks(float seconds) => Math.Max(0, (int)Math.Ceiling(seconds * TickRate - 1e-4));

        // --- Advancing -------------------------------------------------------------

        // Feed real (unscaled) time; returns how many ticks ran. A long frame runs at most
        // MaxTicksPerAdvance ticks and drops the rest, so one hitch can't snowball into
        // ever-longer frames.
        public int Advance(float realDeltaTime)
        {
            ExpireScaleRequests(realDeltaTime);
            if (_timeScale <= 0f) return 0;

            _accumulator += realDeltaTime * _timeScale;

            int ticks = 0;
            while (_accumulator >= TickDuration && ticks < MaxTicksPerAdvance)
            {
                _accumulator -= TickDuration;
                RunTick();
                ticks++;
            }

            if (ticks == MaxTicksPerAdvance && _accumulator >= TickDuration)
                _accumulator = 0.0;

            return ticks;
        }

        // Runs exactly one tick, ignoring pause and scale — frame-by-frame debugging.
        public void Step() => RunTick();

        private void RunTick()
        {
            Tick++;

            if (_tickOrderDirty) RebuildTickOrder();
            foreach (ITickable tickable in _tickOrder)
                tickable.Tick(TickDuration);

            RunDueCalls();
        }

        // Calls scheduled while this runs are due on a later tick, so appending is safe.
        private void RunDueCalls()
        {
            for (int i = 0; i < _scheduled.Count; i++)
            {
                ScheduledCall call = _scheduled[i];
                if (call.IsCancelled || call.DueTick > Tick) continue;

                call.Cancel();
                call.Callback?.Invoke();
            }

            _scheduled.RemoveAll(c => c.IsCancelled);
        }

        private void ExpireScaleRequests(float realDeltaTime)
        {
            for (int i = _scaleRequests.Count - 1; i >= 0; i--)
            {
                TimeScaleRequest request = _scaleRequests[i];
                if (request.RealRemaining <= 0f) continue;

                request.RealRemaining -= realDeltaTime;
                if (request.RealRemaining <= 0f) Release(request);
            }
        }

        private void RefreshTimeScale()
        {
            float scale = 1f;
            if (IsPaused) scale = 0f;
            else foreach (TimeScaleRequest request in _scaleRequests) scale *= request.Scale;

            if (Math.Abs(scale - _timeScale) < 1e-6f) return;

            _timeScale = scale;
            TimeScaleChanged?.Invoke(scale);
        }

        private void RebuildTickOrder()
        {
            _tickables.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));

            _tickOrder = new ITickable[_tickables.Count];
            for (int i = 0; i < _tickables.Count; i++)
                _tickOrder[i] = _tickables[i].Tickable;

            _tickOrderDirty = false;
        }

        private readonly struct Registration
        {
            public Registration(ITickable tickable, int order, int sequence)
            {
                Tickable = tickable;
                Order    = order;
                Sequence = sequence;
            }

            public ITickable Tickable { get; }
            public int       Order    { get; }
            // Registration order breaks ties, so equal orders tick deterministically.
            public int       Sequence { get; }
        }
    }
}
