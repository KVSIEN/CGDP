using System;
using System.Collections.Generic;

namespace CGD.Level
{
    // Keeps a sealed room from becoming a trap. The shutters open when every enemy the room
    // waits on is dead, so one that can never be killed (stuck in geometry, shoved out of the
    // room, on a ledge nobody can reach) would hold the player in for good. Two rules:
    //   - a member that stays stranded (isStranded) for StrandedSeconds is let go: it no longer
    //     counts, though it stays alive;
    //   - if nothing dies for StallSeconds, every member is let go (Overridden).
    // Plain timing, no scene: the encounter decides what stranded means and feeds it in.
    public sealed class SealFailsafe<T> where T : class
    {
        private readonly float _strandedSeconds;
        private readonly float _stallSeconds;
        private Dictionary<T, float> _stranded = new();
        private Dictionary<T, float> _next = new();
        private int _lastCount = -1;
        private float _sinceProgress;

        public SealFailsafe(float strandedSeconds, float stallSeconds)
        {
            _strandedSeconds = Math.Max(0f, strandedSeconds);
            _stallSeconds    = Math.Max(0f, stallSeconds);
        }

        // True once the stall rule has let everyone go.
        public bool Overridden { get; private set; }

        // Moves the clocks on by `deltaTime`. `members`: what the room still waits on. Returns
        // the ones to let go of now (empty most of the time).
        public List<T> Advance(float deltaTime, IReadOnlyList<T> members, Func<T, bool> isStranded)
        {
            var release = new List<T>();
            if (members.Count == 0) return release;

            if (members.Count != _lastCount) _sinceProgress = 0f;
            _lastCount = members.Count;
            _sinceProgress += deltaTime;

            _next.Clear();
            foreach (T member in members)
            {
                if (!isStranded(member)) continue;
                _stranded.TryGetValue(member, out float seconds);
                seconds += deltaTime;
                _next[member] = seconds;
                if (seconds >= _strandedSeconds) release.Add(member);
            }
            (_stranded, _next) = (_next, _stranded);

            if (_sinceProgress >= _stallSeconds)
            {
                Overridden = true;
                return new List<T>(members);
            }
            return release;
        }
    }
}
