using System.Text;

namespace CGD.Telemetry
{
    // The run summary line for the end screen.
    public static class RunStatsText
    {
        public static string Summary(RunStats run)
        {
            var text = new StringBuilder();
            text.Append(run.FormatDuration()).Append(" · ");
            text.Append(run.Kills).Append(run.Kills == 1 ? " kill" : " kills");
            AppendTiers(text, run);
            text.Append(" · ").Append(Rounded(run.DamageDealt)).Append(" dealt · ");
            text.Append(Rounded(run.DamageTaken)).Append(" taken");
            return text.ToString();
        }

        public static string Lifetime(LifetimeStats lifetime) =>
            $"{lifetime.runs} runs · {lifetime.extractions + lifetime.emergencyExtractions} out · {lifetime.deaths} lost · {lifetime.kills} kills";

        private static void AppendTiers(StringBuilder text, RunStats run)
        {
            if (run.Kills == 0) return;
            text.Append(" (T1 ").Append(run.KillsOfTier(1))
                .Append(" T2 ").Append(run.KillsOfTier(2))
                .Append(" T3 ").Append(run.KillsOfTier(3)).Append(')');
        }

        private static int Rounded(float value) => (int)System.Math.Round(value);
    }
}
