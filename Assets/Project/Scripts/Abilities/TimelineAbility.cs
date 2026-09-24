using UnityEngine;
using CGD.Combat;
using CGD.Targeting;

namespace CGD.Abilities
{
    [CreateAssetMenu(fileName = "TimelineAbility", menuName = "CGD/Abilities/Timeline")]
    public class TimelineAbility : Ability
    {
        public ActionTimeline Timeline;

        [Tooltip("Optional. When it yields a point (e.g. a GroundTargetSelector), WorldOffset events are placed there instead of at the player.")]
        public TargetSelector Targeting;

        [Tooltip("AI perception noise emitted when the ability fires (0 = silent).")]
        [Min(0f)]
        public float NoiseRadius;

        private readonly TargetSet _targets = new();

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

            runner.Play(Timeline, ctx, SelectTargetPoint(ctx));

            if (NoiseRadius > 0f)
                Noise.Emit(ctx.PlayerTransform.position, NoiseRadius, ctx.Source);
        }

        private Vector3? SelectTargetPoint(AbilityContext ctx)
        {
            if (Targeting == null || ctx.CameraTransform == null) return null;

            Targeting.Select(new TargetingRequest(ctx.CameraTransform.position, ctx.CameraTransform.forward, ctx.Health), _targets);
            return _targets.HasPoint ? _targets.Point : (Vector3?)null;
        }
    }
}
