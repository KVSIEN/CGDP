using UnityEngine;

namespace CGD.Abilities
{
    // Primal Roar: a buff (more health and damage) that also tells the caster where every
    // hostile in its room is, however well they hide, for as long as the buff lasts. Health the
    // buff adds comes filled, and goes with the buff. On the player it is only the buff:
    // only enemies can be told where someone is.
    [CreateAssetMenu(fileName = "RoarAbility", menuName = "CGD/Abilities/Roar")]
    public class RoarAbility : StatBuffAbility
    {
        [Header("Roar")]
        [Tooltip("Metres: the furthest hostile it learns of (it must also be in the caster's own room)")]
        [Min(1f)] public float RevealRadius = 40f;

        public override void Execute(AbilityContext ctx)
        {
            float healthBefore = ctx.Health != null ? ctx.Health.MaxHealth : 0f;
            base.Execute(ctx);

            if (ctx.Health != null) ctx.Health.Heal(ctx.Health.MaxHealth - healthBefore);
            if (ctx.Scout != null) ctx.Scout.RevealHostiles(RevealRadius, Duration);
        }
    }
}
