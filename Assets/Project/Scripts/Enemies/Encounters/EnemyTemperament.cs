namespace CGD.Enemies
{
    // An enemy's temperament right now: its faction's (TemperamentDoctrine), moved by its
    // situation. Regrouping (its lead fell, or its overseer is holding it back) makes it
    // warier and less eager; having just stepped up to lead makes it bolder. Pure, so it can
    // be tested.
    public readonly struct EnemyTemperament
    {
        private const float RegroupAggression  = 0.6f;
        private const float RegroupCaution     = 1.5f;
        private const float SteppedUpAggression = 1.4f;
        private const float SteppedUpCaution    = 0.8f;

        public readonly float Aggression;
        public readonly float Caution;
        public readonly float Cunning;

        public EnemyTemperament(float aggression, float caution, float cunning)
        {
            Aggression = aggression;
            Caution    = caution;
            Cunning    = cunning;
        }

        public static EnemyTemperament Of(TemperamentDoctrine baseline, bool regrouping, bool steppedUp)
        {
            float aggression = baseline.Aggression;
            float caution    = baseline.Caution;
            if (regrouping)
            {
                aggression *= RegroupAggression;
                caution    *= RegroupCaution;
            }
            if (steppedUp)
            {
                aggression *= SteppedUpAggression;
                caution    *= SteppedUpCaution;
            }
            return new EnemyTemperament(aggression, caution, baseline.Cunning);
        }
    }
}
