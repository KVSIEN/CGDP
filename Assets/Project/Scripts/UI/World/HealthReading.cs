using CGD.Combat;

namespace CGD.UI
{
    // A character's pools at one moment, as a health bar shows them: health (or, for a character
    // that lives on its plating, the plating in its place), any plating on top of health, and
    // the shield.
    public readonly struct HealthReading
    {
        public readonly float Health, MaxHealth;
        public readonly float Shield, MaxShield;
        public readonly float Plating, MaxPlating;
        public readonly bool  LivesOnPlating;

        public HealthReading(float health, float maxHealth, float shield, float maxShield,
                             float plating, float maxPlating, bool livesOnPlating)
        {
            Health         = health;
            MaxHealth      = maxHealth;
            Shield         = shield;
            MaxShield      = maxShield;
            Plating        = plating;
            MaxPlating     = maxPlating;
            LivesOnPlating = livesOnPlating;
        }

        public static HealthReading Of(HealthManager health) =>
            new(health.Health, health.MaxHealth, health.Shield, health.MaxShield,
                health.Plating, health.MaxPlating, health.LivesOnPlating);

        // The main bar: the pool it lives on.
        public float Vital    => LivesOnPlating ? Plating : Health;
        public float MaxVital => LivesOnPlating ? MaxPlating : MaxHealth;
        // Plating shown as its own strip: plates over health (a BIO's bone plating).
        public bool  HasPlatingStrip => !LivesOnPlating && MaxPlating > 0f;
    }
}
