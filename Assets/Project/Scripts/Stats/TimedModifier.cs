using System;

namespace CGD.Stats
{
    // Handle for one group of temporary modifiers. Also the Source of those modifiers,
    // so ending it removes exactly them.
    public sealed class TimedModifier
    {
        private readonly Action<TimedModifier> _expire;

        internal TimedModifier(Action<TimedModifier> expire, float seconds)
        {
            _expire   = expire;
            Duration  = seconds;
            Remaining = seconds;
        }

        public float Duration  { get; }
        public float Remaining { get; internal set; }
        public bool  IsActive  => Remaining > 0f;

        public void End() => _expire(this);
    }
}
