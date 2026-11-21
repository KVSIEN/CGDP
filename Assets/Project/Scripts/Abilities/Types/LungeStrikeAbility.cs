using UnityEngine;

namespace CGD.Abilities
{
    // Bio's Venom Bite: a lunge at the aim point (its target), stopping just short of it, and a
    // strike where it lands: the Strike ability (a TargetedAbility: a short cone, its damage and
    // on-hit effects such as Poison) goes off on arrival, so a target that stepped aside during
    // the lunge is missed. A stun mid-lunge cancels the strike. Needs a caster that can dash on
    // its own and aims at a point (enemies).
    [CreateAssetMenu(fileName = "LungeStrikeAbility", menuName = "CGD/Abilities/Lunge Strike")]
    public class LungeStrikeAbility : Ability
    {
        [Header("Lunge")]
        [Tooltip("Metres it covers at most, unless a wall or ledge stops it sooner")]
        [Min(0.5f)] public float MaxDistance = 6f;
        [Tooltip("Metres short of the aim point it stops, so it lands in front of its target and not on top of it")]
        [Min(0f)] public float StopShort = 1.2f;
        [Tooltip("Seconds the lunge takes")]
        [Min(0.05f)] public float Duration = 0.25f;

        [Header("Strike")]
        [Tooltip("Goes off where it lands, aimed the way it lunged")]
        public TargetedAbility Strike;

        public override bool CanExecute(AbilityContext ctx) =>
            Strike != null && ctx.Dasher != null && !ctx.Dasher.IsDashing && ctx.HasAimPoint && ctx.CasterTransform != null;

        public override void Execute(AbilityContext ctx)
        {
            if (Strike == null || ctx.Dasher == null || ctx.CasterTransform == null) return;

            Vector3 toAim = ctx.AimPoint - ctx.CasterTransform.position;
            toAim.y = 0f;
            float distance = Mathf.Min(MaxDistance, toAim.magnitude - StopShort);
            if (distance <= 0.1f)
            {
                Strike.Execute(ctx);
                return;
            }

            ctx.Dasher.Begin(toAim, distance, Duration, distance, null, _ => Strike.Execute(ctx));
        }
    }
}
