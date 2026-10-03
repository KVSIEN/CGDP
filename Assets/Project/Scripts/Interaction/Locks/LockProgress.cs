using System;
using System.Collections.Generic;

namespace CGD.Interaction
{
    // The rule behind a condition lock, Unity-independent: it opens once `Required`
    // different sources have reported in (two terminals, three signals…). A source that
    // reports twice still counts once, so "A and B" can't be met by doing A twice.
    // Once open it stays open.
    public class LockProgress
    {
        private readonly HashSet<object> _met = new();

        public LockProgress(int required) => Required = Math.Max(1, required);

        public int  Required { get; private set; }
        public int  Met      => _met.Count;
        public bool IsOpen   { get; private set; }

        // Raised on every new source, and once more (after Changed) when the lock opens.
        public event Action Changed;
        public event Action Opened;

        // For locks whose parts are only known at runtime (a generated level's terminals).
        public void SetRequired(int required)
        {
            Required = Math.Max(1, required);
            Evaluate();
        }

        // True when the source is newly counted.
        public bool Report(object source)
        {
            if (source == null || IsOpen || !_met.Add(source)) return false;

            Changed?.Invoke();
            Evaluate();
            return true;
        }

        private void Evaluate()
        {
            if (IsOpen || Met < Required) return;
            IsOpen = true;
            Opened?.Invoke();
        }
    }
}
