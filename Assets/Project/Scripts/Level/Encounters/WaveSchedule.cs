using UnityEngine;

namespace CGD.Level
{
    // A timed defence: one wave at the start, then one every Interval until Duration runs
    // out. Advance reports how many waves came due, so a long frame never skips one.
    public class WaveSchedule
    {
        public WaveSchedule(float duration, float interval)
        {
            Duration = Mathf.Max(0f, duration);
            Interval = Mathf.Max(0.1f, interval);
        }

        public float Duration  { get; }
        public float Interval  { get; }
        public float Elapsed   { get; private set; }
        public int   Spawned   { get; private set; }
        public float Remaining => Mathf.Max(0f, Duration - Elapsed);
        public bool  IsFinished => Elapsed >= Duration;

        // Waves that fit before the end: at 0, Interval, 2×Interval… while < Duration.
        public int TotalWaves => Mathf.Max(1, Mathf.CeilToInt(Duration / Interval));

        public int Advance(float deltaTime)
        {
            if (IsFinished) return 0;
            Elapsed = Mathf.Min(Duration, Elapsed + Mathf.Max(0f, deltaTime));

            int due = Mathf.Min(TotalWaves, Mathf.FloorToInt(Elapsed / Interval) + 1);
            int fresh = due - Spawned;
            Spawned = due;
            return fresh;
        }
    }
}
