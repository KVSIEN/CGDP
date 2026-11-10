using System;

namespace CGD.Telemetry
{
    // What one expedition did, counted from the player's side. Plain data: the recorder
    // feeds it, the run summary and LifetimeStats read it.
    public class RunStats
    {
        // Kills are split by enemy tier (1–3); anything else (a destructible, a rival) counts as "other".
        public const int Tiers = 3;

        private readonly int[] _killsByTier = new int[Tiers];

        public float Seconds         { get; private set; }
        public float DamageDealt     { get; private set; }
        public float DamageTaken     { get; private set; }
        public int   Hits            { get; private set; }
        public int   CriticalHits    { get; private set; }
        public int   Kills           { get; private set; }
        public int   KillsOther      { get; private set; }
        public int   ItemsCollected  { get; private set; }

        public int KillsOfTier(int tier) => tier >= 1 && tier <= Tiers ? _killsByTier[tier - 1] : 0;

        public void AddTime(float seconds)
        {
            if (seconds > 0f) Seconds += seconds;
        }

        // tier: the enemy's tier when the target was an enemy, 0 for anything else.
        public void RecordDealt(float amount, bool critical, bool killed, int tier)
        {
            if (amount > 0f)
            {
                DamageDealt += amount;
                Hits++;
                if (critical) CriticalHits++;
            }

            if (!killed) return;
            Kills++;
            if (tier >= 1 && tier <= Tiers) _killsByTier[tier - 1]++;
            else KillsOther++;
        }

        public void RecordTaken(float amount)
        {
            if (amount > 0f) DamageTaken += amount;
        }

        public void RecordCollected(int count)
        {
            if (count > 0) ItemsCollected += count;
        }

        // Share of hits that were critical, 0 with no hits.
        public float CriticalRate => Hits > 0 ? (float)CriticalHits / Hits : 0f;

        public string FormatDuration() => FormatDuration(Seconds);

        public static string FormatDuration(float seconds)
        {
            var span = TimeSpan.FromSeconds(Math.Max(0f, seconds));
            return span.TotalHours >= 1d
                ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
                : $"{span.Minutes}:{span.Seconds:00}";
        }
    }
}
