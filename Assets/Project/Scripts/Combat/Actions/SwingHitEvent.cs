using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Impacts;

namespace CGD.Combat
{
    // A blade (capsule) that swings from a start pose to an end pose across the event's frames.
    // Each tick it checks the capsule where it is now and, when it moved far, at a few poses
    // in between — so a fast slash can't skip over a target and a slow one costs a single
    // overlap query. Hits resolve hitbox regions, so headshots and limb hits still apply.
    //
    //   Slash:  Start Yaw 60  → End Yaw -60      Thrust: both poses Yaw 0, Push 0 → 1.2
    //   Chop:   Start Pitch 70 → End Pitch -10   Stab upward: Pitch 45, Push 0 → 1
    //
    // Turn on the weapon's debug draw to see the capsule's trail in the scene view.
    [Serializable]
    public class SwingHitEvent : IActionEvent
    {
        private const int MaxSubSteps = 6;
        private const float SubStepSpacing = 1.5f; // × radius: neighbouring capsules always overlap

        private static readonly Collider[]   OverlapBuffer = new Collider[32];
        private static readonly IDamageable[] TargetBuffer = new IDamageable[32];
        private static readonly float[]      ScoreBuffer   = new float[32];
        private static readonly Vector3[]    PointBuffer   = new Vector3[32];

        [Header("Frames")]
        [SerializeField] private int _startFrame;
        [Tooltip("Last frame of the swing; the blade reaches its end pose here.")]
        [SerializeField] private int _endFrame = 5;

        [Header("Blade")]
        [Tooltip("Where the blade begins and ends along its direction (metres from the aim origin). A sword is roughly 0.3 → 1.8.")]
        [SerializeField] private float _nearDistance = 0.3f;
        [SerializeField] private float _farDistance = 1.8f;
        [Tooltip("Thickness of the hit volume. Wider = more forgiving.")]
        [Min(0.01f)]
        [SerializeField] private float _radius = 0.25f;
        [Tooltip("Shifts the swing's pivot from the aim origin: x right, y up, z forward.")]
        [SerializeField] private Vector3 _pivotOffset;

        [Header("Motion")]
        [Tooltip("Yaw: + right / − left. Pitch: + up / − down. Push: slides the blade forward (thrusts).")]
        [SerializeField] private SwingPose _start = new(60f);
        [SerializeField] private SwingPose _end = new(-60f);
        [Tooltip("Pace of the swing: time along the bottom, progress between poses up the side. Ease-in makes a wind-up whip.")]
        [SerializeField] private AnimationCurve _pace = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Damage")]
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
            float span = EndFrame - _startFrame;
            float t     = Mathf.Clamp01((ctx.Frame - _startFrame) / span);
            float prevT = Mathf.Clamp01((ctx.Frame - 1 - _startFrame) / span);

            Quaternion aim = Quaternion.LookRotation(ctx.Forward, ctx.Up);
            Vector3 pivot = ctx.Origin + aim * _pivotOffset;

            SwingBlade from = BladeAt(pivot, aim, prevT);
            SwingBlade to   = BladeAt(pivot, aim, t);
            int steps = SwingBlade.StepsBetween(from, to, _radius * SubStepSpacing, MaxSubSteps);

            HashSet<IDamageable> dedup = _useSharedDedup ? ctx.SharedHits : ctx.EventHits[ctx.CurrentEventIndex];
            bool hasInfo = false;
            DamageInfo info = default;

            for (int i = 1; i <= steps; i++)
            {
                SwingBlade blade = BladeAt(pivot, aim, Mathf.Lerp(prevT, t, i / (float)steps));
                int count = Physics.OverlapCapsuleNonAlloc(
                    blade.Near, blade.Far, _radius, OverlapBuffer,
                    ctx.HitMask, QueryTriggerInteraction.Ignore);

                if (ctx.DebugDraw)
                    DebugDrawUtil.Capsule(blade.Near, blade.Far, _radius, Color.magenta, ctx.DebugDuration);

                if (count == 0) continue;
                if (!hasInfo)
                {
                    info = BuildDamageInfo(ctx);
                    hasInfo = true;
                }
                ApplyHits(ctx, blade, count, info, dedup);
            }
        }

        private SwingBlade BladeAt(Vector3 pivot, Quaternion aim, float time)
        {
            float progress = _pace.Evaluate(time);
            return SwingBlade.At(pivot, aim, SwingPose.Lerp(_start, _end, progress), _nearDistance, _farDistance);
        }

        private DamageInfo BuildDamageInfo(ActionContext ctx) =>
            ctx.Prepare(new DamageInfo(
                _damage, _armorPenetration, _damageType,
                _criticalMultiplier, ctx.Source, _onHitEffects));

        // Damages each new target once. When several of a target's hitboxes overlap the blade,
        // the one the blade's axis runs deepest into takes the hit, so the region struck
        // doesn't depend on physics query order.
        private static void ApplyHits(ActionContext ctx, SwingBlade blade, int count,
                                      DamageInfo info, HashSet<IDamageable> dedup)
        {
            for (int i = 0; i < count; i++)
            {
                Collider col = OverlapBuffer[i];
                TargetBuffer[i] = col.transform.root == ctx.SourceRoot ? null : Hitbox.FindDamageable(col);
                if (TargetBuffer[i] == null) continue;

                Vector3 onAxis = ClosestOnSegment(blade.Near, blade.Far, col.bounds.center);
                PointBuffer[i] = col.ClosestPoint(onAxis);
                ScoreBuffer[i] = (PointBuffer[i] - onAxis).sqrMagnitude;
            }

            for (int i = 0; i < count; i++)
            {
                IDamageable target = TargetBuffer[i];
                if (target == null || dedup.Contains(target)) continue;

                int best = i;
                for (int j = i + 1; j < count; j++)
                    if (TargetBuffer[j] == target && ScoreBuffer[j] < ScoreBuffer[best]) best = j;

                dedup.Add(target);
                Collider hit = OverlapBuffer[best];
                Vector3 point = PointBuffer[best];

                Hitbox.ApplyHit(hit, info, point);
                ImpactEvents.Report(point, SurfaceNormal(ctx, blade, point), hit, ImpactKind.Melee);
                ctx.HitAnything = true;
            }
        }

        private static Vector3 SurfaceNormal(ActionContext ctx, SwingBlade blade, Vector3 point)
        {
            Vector3 away = ClosestOnSegment(blade.Near, blade.Far, point) - point;
            if (away.sqrMagnitude > 0.0001f) return away.normalized;
            return -ctx.Forward;
        }

        private static Vector3 ClosestOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float lengthSqr = ab.sqrMagnitude;
            if (lengthSqr < 0.0001f) return a;
            return a + ab * Mathf.Clamp01(Vector3.Dot(p - a, ab) / lengthSqr);
        }
    }
}
