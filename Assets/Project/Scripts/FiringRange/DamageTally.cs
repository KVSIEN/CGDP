namespace CGD.FiringRange
{
    // The numbers for one "fight" on the firing range: a run of hits on a target, until it
    // dies or you stop shooting for a few seconds. Plain C# so the rules can be tested;
    // DamageReadout feeds it hits and shows the result.
    public class DamageTally
    {
        // Pause in shooting after which the next hit counts as a new fight.
        public const float IdleReset = 3f;

        private float _firstTime;
        private float _lastTime;
        private float _lastAmount;

        public int   Hits      { get; private set; }
        public int   Headshots { get; private set; }
        public float Damage    { get; private set; }
        public bool  Killed    { get; private set; }

        // Seconds from the first hit of the fight to the latest one.
        public float Duration => Hits > 0 ? _lastTime - _firstTime : 0f;

        // Sustained rate: each hit is counted over the gap that follows it, so the final hit
        // (nothing after it yet) is left out and a single hit has no rate.
        public float DamagePerSecond => Duration > 0f ? (Damage - _lastAmount) / Duration : 0f;

        public void Record(float time, float amount, bool headshot, bool killed)
        {
            if (Hits > 0 && (Killed || time - _lastTime > IdleReset)) Clear();
            if (Hits == 0) _firstTime = time;

            Hits++;
            if (headshot) Headshots++;
            Damage     += amount;
            Killed      = killed;
            _lastTime   = time;
            _lastAmount = amount;
        }

        public void Clear()
        {
            Hits = Headshots = 0;
            Damage = _lastAmount = 0f;
            Killed = false;
        }
    }
}
