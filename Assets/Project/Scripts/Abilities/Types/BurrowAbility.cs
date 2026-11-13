using UnityEngine;

namespace CGD.Abilities
{
    // Dives underground and erupts beside its target: the cast time is the dig (show it with a
    // ground ring — the caster's AbilityLook), then it surfaces within a couple of metres of where
    // it believes the target is and bursts out like a shockwave, hurting and shoving everyone
    // close. Enemies only (it needs a caster that can relocate); fails while the target is out
    // of reach or there is nowhere walkable beside it.
    [CreateAssetMenu(fileName = "BurrowAbility", menuName = "CGD/Abilities/Burrow")]
    public class BurrowAbility : ShockwaveAbility
    {
        [Header("Burrow")]
        [Tooltip("Metres: the furthest target it will burrow to")]
        [Min(1f)] public float MaxDistance = 25f;
        [Tooltip("Metres from the target it surfaces within")]
        [Min(0.5f)] public float SurfaceWithin = 2f;

        public override bool CanExecute(AbilityContext ctx) => ctx.Relocator != null;

        public override void Execute(AbilityContext ctx)
        {
            if (ctx.Relocator == null || !ctx.Relocator.TryRelocateNearTarget(MaxDistance, SurfaceWithin)) return;
            base.Execute(ctx);   // the eruption, where it surfaced
        }
    }
}
