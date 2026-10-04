namespace CGD.Weapons
{
    // Which light-combo step a weapon swings next, and until when that step stays open.
    // Times are absolute game time, so a weapon that is switched away keeps counting down
    // without being ticked, and continues where it left off if switched back in time.
    public class ComboState
    {
        private int   _next;
        private float _openUntil = float.NegativeInfinity;

        public bool IsOpen(float now) => now <= _openUntil;

        // The step to swing at `now`: the next one while the combo is open, else the first.
        public int StepAt(float now) => IsOpen(now) ? _next : 0;

        // A light step started; the step after it stays open until the swing ends.
        public void Begin(int step, int stepCount)
        {
            _next      = (step + 1) % stepCount;
            _openUntil = float.PositiveInfinity;
        }

        // The swing ended (or was cancelled keeping the combo): the next step waits `duration`.
        public void Release(float now, float duration)
        {
            if (IsOpen(now)) _openUntil = now + duration;
        }

        // A weave action (dodge, parry, ability, weapon switch) between steps: keeps an open
        // combo going for at least `duration` from now. A closed combo stays closed.
        public void Extend(float now, float duration)
        {
            if (IsOpen(now) && now + duration > _openUntil) _openUntil = now + duration;
        }

        public void Reset()
        {
            _next      = 0;
            _openUntil = float.NegativeInfinity;
        }
    }
}
