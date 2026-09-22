using UnityEngine;
using CGD.Combat;

namespace CGD.Abilities
{
    [CreateAssetMenu(fileName = "TimelineAbility", menuName = "CGD/Abilities/Timeline")]
    public class TimelineAbility : Ability
    {
        public ActionTimeline Timeline;

        [Tooltip("AI perception noise emitted when the ability fires (0 = silent).")]
        [Min(0f)]
        public float NoiseRadius;

        public override bool CanExecute(AbilityContext ctx)
        {
            if (Timeline == null) return false;
            var runner = ctx.PlayerTransform.GetComponent<TimelineAbilityRunner>();
            return runner != null && !runner.IsPlaying;
        }

        public override void Execute(AbilityContext ctx)
        {
            var runner = ctx.PlayerTransform.GetComponent<TimelineAbilityRunner>();
            if (runner == null) return;

            runner.Play(Timeline, ctx);

            if (NoiseRadius > 0f)
                Noise.Emit(ctx.PlayerTransform.position, NoiseRadius, ctx.Source);
        }
    }
}
