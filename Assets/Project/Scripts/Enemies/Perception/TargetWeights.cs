using UnityEngine;

namespace CGD.Enemies
{
    // How much more an enemy wants to fight one hostile than another, on top of how strongly
    // it senses each (closer and in plain view counts most). Every weight multiplies, so a
    // weight of 0 ignores that trait and the default only keeps it focused on its target.
    public readonly struct TargetWeights
    {
        public static readonly TargetWeights Default = new(focus: 1.25f, retaliation: 0f, wounded: 0f, distracted: 0f);

        // ×focus for the hostile it is already fighting: switching needs a clearly better one.
        public readonly float Focus;
        // ×(1 + retaliation) for whoever just hurt it.
        public readonly float Retaliation;
        // ×(1 + wounded × share missing): a hostile at half health with wounded 1 counts 1.5×.
        public readonly float Wounded;
        // ×(1 + distracted) for a hostile busy fighting someone else.
        public readonly float Distracted;

        public TargetWeights(float focus, float retaliation, float wounded, float distracted)
        {
            Focus       = Mathf.Max(1f, focus);
            Retaliation = Mathf.Max(0f, retaliation);
            Wounded     = Mathf.Max(0f, wounded);
            Distracted  = Mathf.Max(0f, distracted);
        }

        public float Weigh(in TargetTraits traits)
        {
            float weight = 1f + Wounded * (1f - Mathf.Clamp01(traits.HealthShare));
            if (traits.IsCurrent)      weight *= Focus;
            if (traits.HurtItRecently) weight *= 1f + Retaliation;
            if (traits.IsDistracted)   weight *= 1f + Distracted;
            return weight;
        }
    }
}
