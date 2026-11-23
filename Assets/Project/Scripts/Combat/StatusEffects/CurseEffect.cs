using UnityEngine;

namespace CGD.Combat
{
    // A curse on the target: while it lasts, a share of every hit the target takes — from anyone —
    // lands as true damage, which armour does nothing against. Each stack adds SharePerStack (× the
    // source's Potency), up to MaxShare. Passive: HealthManager reads the share through
    // StatusEffectController.TrueDamageShare as each hit resolves, so Tick() has nothing to do.
    [CreateAssetMenu(fileName = "CurseEffect", menuName = "CGD/Combat/Status Effects/Curse")]
    public class CurseEffect : StatusEffect
    {
        [Header("Curse")]
        [Tooltip("Share of each hit taken that becomes true damage, per stack (0.08 = 8%)")]
        [Range(0f, 1f)] public float SharePerStack = 0.08f;
        [Tooltip("The most the curse can convert, whatever the stacks and potency (0.5 = half of every hit)")]
        [Range(0f, 1f)] public float MaxShare = 0.5f;

        public float ShareOf(int stacks, float potency) => Mathf.Min(stacks * SharePerStack * potency, MaxShare);

        public override void Tick(StatusEffectController target, int stacks, float magnitude) { }
    }
}
