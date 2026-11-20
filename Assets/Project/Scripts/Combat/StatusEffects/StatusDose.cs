namespace CGD.Combat
{
    // How strongly and for how long one source applies an effect, on top of the effect asset's
    // own tuning, so many weapons and abilities can share one Bleed, one Slow, one Stun... and
    // still hit differently. Each effect decides what Potency scales (its damage, its slow).
    public readonly struct StatusDose
    {
        public readonly float Potency;  // 1 = as the asset is tuned
        public readonly float Duration; // seconds; 0 = the asset's own Duration

        public StatusDose(float potency, float duration = 0f)
        {
            Potency  = potency < 0f ? 0f : potency;
            Duration = duration < 0f ? 0f : duration;
        }

        // The asset exactly as tuned.
        public static StatusDose Full => new(1f);

        public float DurationOf(StatusEffect effect) => Duration > 0f ? Duration : effect.Duration;
    }
}
