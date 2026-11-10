using UnityEngine;

namespace CGD.Enemies
{
    // How much more an enemy wants to fight one hostile than another, on top of how strongly
    // it senses each (closer and in plain view counts most). Every weight multiplies, so a
    // weight of 0 ignores that trait and the default only keeps it focused on its target.
    public readonly struct TargetWeights
    {
        public static readonly TargetWeights Default = new(focus: 1.25f, retaliation: 0f, wounded: 0f, distracted: 0f, finish: 0f);

        // ×focus for the hostile it is already fighting: switching needs a clearly better one.
        public readonly float Focus;
        // ×(1 + finish × share missing) on top, for the one it is fighting only: the more of it
        // is already worn away, the more it is worth seeing through rather than starting again
        // on someone fresh. A weight like the rest, so a pressing enough threat still wins.
        public readonly float Finish;
        // ×(1 + retaliation) for whoever just hurt it.
        public readonly float Retaliation;
        // ×(1 + wounded × share missing): a hostile at half health with wounded 1 counts 1.5×.
        public readonly float Wounded;
        // ×(1 + distracted) for a hostile busy fighting someone else.
        public readonly float Distracted;

        public TargetWeights(float focus, float retaliation, float wounded, float distracted, float finish = 0f)
        {
            Focus       = Mathf.Max(1f, focus);
            Retaliation = Mathf.Max(0f, retaliation);
            Wounded     = Mathf.Max(0f, wounded);
            Distracted  = Mathf.Max(0f, distracted);
            Finish      = Mathf.Max(0f, finish);
        }

        public float Weigh(in TargetTraits traits)
        {
            float missing = 1f - Mathf.Clamp01(traits.HealthShare);
            float weight  = 1f + Wounded * missing;
            if (traits.IsCurrent)      weight *= Focus * (1f + Finish * missing);
            if (traits.HurtItRecently) weight *= 1f + Retaliation;
            if (traits.IsDistracted)   weight *= 1f + Distracted;
            return weight;
        }
    }
}
