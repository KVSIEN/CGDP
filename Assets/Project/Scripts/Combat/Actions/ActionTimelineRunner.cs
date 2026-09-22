using UnityEngine;

namespace CGD.Combat
{
    public class ActionTimelineRunner
    {
        private ActionTimeline _timeline;
        private ActionContext _ctx;
        private int _frame;
        private bool[] _active;
        private bool _running;

        public bool IsRunning => _running;
        public bool HitAnything => _ctx != null && _ctx.HitAnything;

        public void Begin(ActionTimeline timeline, ActionContext ctx)
        {
            _timeline = timeline;
            _ctx = ctx;
            _frame = -1;
            _running = true;

            int count = timeline.Events != null ? timeline.Events.Length : 0;
            if (_active == null || _active.Length < count)
                _active = new bool[count];
            for (int i = 0; i < count; i++)
                _active[i] = false;

            ctx.Clear(count);
        }

        public void Tick()
        {
            if (!_running) return;

            _frame++;

            if (_frame >= _timeline.TotalFrames)
            {
                Stop();
                return;
            }

            if (_timeline.NoiseFrame >= 0 && _frame == _timeline.NoiseFrame)
                Noise.Emit(_ctx.Origin, _timeline.NoiseRadius, _ctx.Source);

            if (_timeline.Events == null) return;

            for (int i = 0; i < _timeline.Events.Length; i++)
            {
                IActionEvent evt = _timeline.Events[i];
                if (evt == null) continue;

                _ctx.CurrentEventIndex = i;
                bool isSingleFrame = evt.EndFrame < 0;

                if (_frame == evt.StartFrame)
                {
                    _active[i] = true;
                    evt.OnEnter(_ctx);

                    if (isSingleFrame)
                    {
                        evt.OnExit(_ctx);
                        _active[i] = false;
                        continue;
                    }
                }

                if (_active[i])
                {
                    evt.OnTick(_ctx);

                    if (_frame >= evt.EndFrame)
                    {
                        evt.OnExit(_ctx);
                        _active[i] = false;
                    }
                }
            }
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;

            if (_timeline?.Events == null) return;

            for (int i = 0; i < _timeline.Events.Length; i++)
            {
                if (!_active[i]) continue;
                _ctx.CurrentEventIndex = i;
                _timeline.Events[i]?.OnExit(_ctx);
                _active[i] = false;
            }
        }
    }
}
