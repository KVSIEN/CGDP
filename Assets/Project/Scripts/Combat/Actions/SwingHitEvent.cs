using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Impacts;

namespace CGD.Combat
{
    // A blade (capsule) that swings along keyed poses across the event's frames. Each tick it
    // checks the capsule where it is now and, when it moved far, at a few poses in between —
    // so a fast slash can't skip over a target and a slow one costs a single overlap query.
    // Hits resolve hitbox regions, so headshots and limb hits still apply.
    //
    // Add a key per pose the blade should pass through (frames count from the event's start):
    //   Slash:  frame 0 Yaw 60,  frame 5 Yaw -60
    //   Chop:   frame 0 Pitch 70, frame 2 Pitch 70 (hold), frame 5 Pitch -10
    //   Thrust: frame 0 Push 0,   frame 4 Push 1.2
    // Anchor = Socket makes the swing follow the weapon's socket instead of the aim, so the
    // animation can carry the hitbox. Turn on the weapon's debug draw to see the capsule's trail.
    [Serializable]
    public class SwingHitEvent : IActionEvent
    {
        private const int MaxSubSteps = 6;
        private const float SubStepSpacing = 1.5f; // × radius: neighbouring capsules always overlap

        private static readonly ShapeHits Shapes = new(32);

        [Header("Frames")]
        [SerializeField] private int _startFrame;
        [Tooltip("Last frame the blade is active.")]
        [SerializeField] private int _endFrame = 5;

        [Header("Blade")]
        [Tooltip("Where the blade begins and ends along its direction (metres from the aim origin). A sword is roughly 0.3 → 1.8.")]
        [SerializeField] private float _nearDistance = 0.3f;
        [SerializeField] private float _farDistance = 1.8f;
        [Tooltip("The weapon reach this swing is drawn for (metres). When a weapon's real reach differs (reach rolls, perks) the whole swing scales to match. 0 = never scale.")]
        [Min(0f)]
        [SerializeField] private float _authoredReach = 1.8f;
        [Tooltip("Thickness of the hit volume. Wider = more forgiving.")]
        [Min(0.01f)]
        [SerializeField] private float _radius = 0.25f;
        [Tooltip("Shifts the swing's pivot from its anchor: x right, y up, z forward.")]
        [SerializeField] private Vector3 _pivotOffset;

        [Header("Motion")]
        [Tooltip("Aim: swing measured from where the wielder looks. Socket: from the weapon socket (its forward is the blade's direction), so an animated weapon carries the hitbox.")]
        [SerializeField] private SwingAnchor _anchor = SwingAnchor.Aim;
        [Tooltip("Poses the blade passes through, in frame order (frames count from the event's start). Yaw: + right / − left. Pitch: + up / − down. Push: slides the blade forward (thrusts). Blends in straight lines — put keys closer together for a faster move.")]
        [SerializeField] private SwingKey[] _keys =
        {
            new(0, new SwingPose(60f)),
            new(5, new SwingPose(-60f)),
        };

        [Header("Damage")]
        [Tooltip("As a weapon's attack: a multiple of the attack step's damage (1 = the weapon's full rolled damage), and the step's own armor penetration, crits, type and effects replace the ones below.")]
        [SerializeField] private float _damage = 20f;
        [Range(0f, 1f)]
        [SerializeField] private float _armorPenetration;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private float _criticalMultiplier = 1.5f;
        [SerializeField] private StatusEffectApplication[] _onHitEffects;

        [Header("Behavior")]
        [Tooltip("True = one hit per target across all events in the timeline. False = per-event dedup.")]
        [SerializeField] private bool _useSharedDedup = true;

        public int StartFrame => _startFrame;
        public int EndFrame => Mathf.Max(_endFrame, _startFrame + 1);

        public void OnEnter(ActionContext ctx) { }
        public void OnExit(ActionContext ctx) { }

        public void OnTick(ActionContext ctx)
        {
            PoseTrail trail = _anchor == SwingAnchor.Socket && ctx.Socket != null ? ctx.SocketTrail : ctx.AimTrail;
            float scale     = ctx.Reach > 0f && _authoredReach > 0f ? ctx.Reach / _authoredReach : 1f;
            float frame     = ctx.Frame - _startFrame;
            float prevFrame = Mathf.Max(frame - 1f, 0f);

            SwingBlade from = BladeAt(trail.Previous, prevFrame, scale);
            SwingBlade to   = BladeAt(trail.Current, frame, scale);
            int steps = SwingBlade.StepsBetween(from, to, _radius * SubStepSpacing, MaxSubSteps);

            HashSet<IDamageable> dedup = _useSharedDedup ? ctx.SharedHits : ctx.EventHits[ctx.CurrentEventIndex];
            bool hasInfo = false;
            DamageInfo info = default;

            for (int i = 1; i <= steps; i++)
            {
                float progress = i / (float)steps;
                SwingBlade blade = BladeAt(trail.Blend(progress), Mathf.Lerp(prevFrame, frame, progress), scale);
                Shapes.Clear();
                Shapes.Capsule(blade.Near, blade.Far, _radius, ctx.HitMask, ctx.SourceRoot, dedup);

                if (ctx.DebugDraw)
                    DebugDrawUtil.Capsule(blade.Near, blade.Far, _radius, Color.magenta, ctx.DebugDuration);

                if (Shapes.Hits.Count == 0) continue;
                if (!hasInfo)
                {
                    info = BuildDamageInfo(ctx);
                    hasInfo = true;
                }
                ApplyHits(ctx, info);
            }
        }

        private SwingBlade BladeAt(Pose anchor, float frame, float scale)
        {
            Vector3 pivot = anchor.position + anchor.rotation * _pivotOffset;
            SwingPose pose = SwingPath.Sample(_keys, frame);
            pose.Push *= scale;
            return SwingBlade.At(pivot, anchor.rotation, pose, _nearDistance * scale, _farDistance * scale);
        }

        private DamageInfo BuildDamageInfo(ActionContext ctx) =>
            ctx.HasStrike ? ctx.Strike.WithDamageScale(_damage) : ctx.Prepare(new DamageInfo(
                _damage, _armorPenetration, _damageType,
                _criticalMultiplier, ctx.Source, _onHitEffects));

        // Each target once, through the hitbox the blade runs deepest into (ShapeHits.Capsule).
        private static void ApplyHits(ActionContext ctx, DamageInfo info)
        {
            foreach (ShapeHit hit in Shapes.Hits)
            {
                Vector3 normal = hit.Normal.sqrMagnitude > 0.0001f ? hit.Normal.normalized : -ctx.Forward;
                Hitbox.ApplyHit(hit.Collider, info, hit.Point);
                ImpactEvents.Report(hit.Point, normal, hit.Collider, ImpactKind.Melee);
            }
            ctx.HitAnything = true;
        }
    }
}
