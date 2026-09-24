using System;

namespace CGD.Timing
{
    // A callback GameClock runs on a specific tick. Cancel it to stop it firing.
    public sealed class ScheduledCall
    {
        internal ScheduledCall(long dueTick, Action callback)
        {
            DueTick  = dueTick;
            Callback = callback;
        }

        public long DueTick     { get; }
        public bool IsCancelled { get; private set; }

        internal Action Callback { get; }

        public void Cancel() => IsCancelled = true;
    }
}
