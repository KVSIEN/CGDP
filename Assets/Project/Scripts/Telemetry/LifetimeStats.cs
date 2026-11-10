using System;
using CGD.Expedition;

namespace CGD.Telemetry
{
    // Totals over every run, kept on disk by TelemetryStore. Serialized with JsonUtility, so
    // plain public fields.
    [Serializable]
    public class LifetimeStats
    {
        // Bump when the layout changes, and teach TelemetryStore to read the old one.
        public const int CurrentVersion = 1;

        public int   version = CurrentVersion;
        public int   runs;
        public int   extractions;
        public int   emergencyExtractions;
        public int   deaths;
        public int   kills;
        public float damageDealt;
        public float damageTaken;
        public float secondsPlayed;
        public int   bestKillsInRun;
        public float longestRunSeconds;

        public void Add(RunStats run, RunOutcome outcome)
        {
            runs++;
            switch (outcome)
            {
                case RunOutcome.Extracted:          extractions++;          break;
                case RunOutcome.EmergencyExtracted: emergencyExtractions++; break;
                default:                            deaths++;               break;
            }

            kills         += run.Kills;
            damageDealt   += run.DamageDealt;
            damageTaken   += run.DamageTaken;
            secondsPlayed += run.Seconds;
            bestKillsInRun    = Math.Max(bestKillsInRun, run.Kills);
            longestRunSeconds = Math.Max(longestRunSeconds, run.Seconds);
        }

        // Share of runs that made it out (an emergency exit counts), 0 before the first run.
        public float SurvivalRate => runs > 0 ? (float)(extractions + emergencyExtractions) / runs : 0f;
    }
}
