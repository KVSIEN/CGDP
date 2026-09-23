using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    [Serializable]
    public class ShapeHitEvent : IActionEvent
    {
        private static readonly RaycastHit[] CastBuffer = new RaycastHit[16];
        private static readonly Collider[] OverlapBuffer = new Collider[16];

        [SerializeField] private int _startFrame;
        [SerializeField] private int _endFrame = -1;

        [Header("Shape")]
        [SerializeField] private HitShape _shape = HitShape.SphereCast;
        [SerializeField] private ActionSpace _space = ActionSpace.CameraRelative;
        [SerializeField] private float _range = 1.8f;
        [SerializeField] private float _radius = 0.25f;
        [SerializeField] private Vector3 _offset;

        [Header("Arc")]
        [Range(10f, 360f)]
        [SerializeField] private float _arcDeg = 90f;
        [Range(2, 24)]
        [SerializeField] private int _rayCount = 5;

        [Header("Box")]
        [SerializeField] private Vector3 _halfExtents = new(0.5f, 0.5f, 0.5f);

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
        [Tooltip("True = Hitbox.ApplyHit with region resolution (point hits). False = FindDamageable + TakeDamage (area hits).")]
        [SerializeField] private bool _usePointHit = true;

        public int StartFrame => _startFrame;
        public int EndFrame => _endFrame;

        public void OnEnter(ActionContext ctx) { }
        public void OnExit(ActionContext ctx) { }

        public void OnTick(ActionContext ctx)
        {
            ResolveOriginAndForward(ctx, out Vector3 origin, out Vector3 forward, out Vector3 up);

            switch (_shape)
            {
                case HitShape.SphereCast:
                    TickSphereCast(ctx, origin, forward);
                    break;
                case HitShape.Arc:
                    TickArc(ctx, origin, forward, up);
                    break;
                case HitShape.Sphere:
                    TickSphere(ctx, origin, forward);
                    break;
                case HitShape.Box:
                    TickBox(ctx, origin, forward);
                    break;
            }
        }

        private void ResolveOriginAndForward(ActionContext ctx,
            out Vector3 origin, out Vector3 forward, out Vector3 up)
        {
            forward = ctx.Forward;
            up = ctx.Up;

            switch (_space)
            {
                case ActionSpace.CameraRelative:
                    Vector3 right = Vector3.Cross(up, forward).normalized;
                    origin = ctx.Origin
                           + right * _offset.x
                           + up * _offset.y
                           + forward * _offset.z;
                    break;

                case ActionSpace.WorldOffset:
                    origin = (ctx.HasTarget ? ctx.TargetPoint : ctx.Origin) + _offset;
                    forward = Vector3.forward;
                    up = Vector3.up;
                    break;

                case ActionSpace.WorldAbsolute:
                    origin = _offset;
                    forward = Vector3.forward;
                    up = Vector3.up;
                    break;

                default:
                    origin = ctx.Origin;
                    break;
            }
        }

        private DamageInfo BuildDamageInfo(ActionContext ctx)
        {
            return new DamageInfo(
                _damage, _armorPenetration, _damageType,
                _criticalMultiplier, ctx.Source, _onHitEffects);
        }

        private HashSet<IDamageable> GetDedup(ActionContext ctx)
        {
            return _useSharedDedup ? ctx.SharedHits : ctx.EventHits[ctx.CurrentEventIndex];
        }

        private void TickSphereCast(ActionContext ctx, Vector3 origin, Vector3 forward)
        {
            int count = Physics.SphereCastNonAlloc(
                origin, _radius, forward, CastBuffer,
                _range, ctx.HitMask, QueryTriggerInteraction.Ignore);

            ProcessCastHits(ctx, count);

            if (ctx.DebugDraw)
                Debug.DrawRay(origin, forward * _range, Color.red, ctx.DebugDuration);
        }

        private void TickArc(ActionContext ctx, Vector3 origin, Vector3 forward, Vector3 up)
        {
            float halfArc = _arcDeg * 0.5f;
            int rays = Mathf.Max(_rayCount, 2);

            for (int r = 0; r < rays; r++)
            {
                float t = (float)r / (rays - 1);
                float angle = Mathf.Lerp(-halfArc, halfArc, t);
                Vector3 dir = Quaternion.AngleAxis(angle, up) * forward;

                int count = Physics.SphereCastNonAlloc(
                    origin, _radius, dir, CastBuffer,
                    _range, ctx.HitMask, QueryTriggerInteraction.Ignore);

                ProcessCastHits(ctx, count);

                if (ctx.DebugDraw)
                    Debug.DrawRay(origin, dir * _range, Color.yellow, ctx.DebugDuration);
            }
        }

        private void TickSphere(ActionContext ctx, Vector3 origin, Vector3 forward)
        {
            Vector3 center = origin + forward * _range;

            int count = Physics.OverlapSphereNonAlloc(
                center, _radius, OverlapBuffer,
                ctx.HitMask, QueryTriggerInteraction.Ignore);

            ProcessOverlapHits(ctx, count);

            if (ctx.DebugDraw)
                DebugDrawUtil.Sphere(center, _radius, Color.cyan, ctx.DebugDuration);
        }

        private void TickBox(ActionContext ctx, Vector3 origin, Vector3 forward)
        {
            Vector3 center = origin + forward * _range;
            Quaternion orientation = Quaternion.LookRotation(forward, ctx.Up);

            int count = Physics.OverlapBoxNonAlloc(
                center, _halfExtents, OverlapBuffer,
                orientation, ctx.HitMask, QueryTriggerInteraction.Ignore);

            ProcessOverlapHits(ctx, count);

            if (ctx.DebugDraw)
                DebugDrawUtil.Box(center, _halfExtents, orientation, Color.green, ctx.DebugDuration);
        }

        private void ProcessCastHits(ActionContext ctx, int count)
        {
            DamageInfo info = BuildDamageInfo(ctx);
            HashSet<IDamageable> dedup = GetDedup(ctx);

            for (int i = 0; i < count; i++)
            {
                Collider col = CastBuffer[i].collider;
                if (col.transform.root == ctx.SourceRoot) continue;

                if (_usePointHit)
                {
                    IDamageable target = Hitbox.FindDamageable(col);
                    if (target == null || !dedup.Add(target)) continue;

                    Vector3 point = CastBuffer[i].point;
                    if (point == Vector3.zero)
                        point = col.ClosestPoint(ctx.SourceRoot.position);

                    Hitbox.ApplyHit(col, info, point);
                }
                else
                {
                    IDamageable target = Hitbox.FindDamageable(col);
                    if (target == null || !dedup.Add(target)) continue;
                    target.TakeDamage(info);
                }

                ctx.HitAnything = true;
            }
        }

        private void ProcessOverlapHits(ActionContext ctx, int count)
        {
            DamageInfo info = BuildDamageInfo(ctx);
            HashSet<IDamageable> dedup = GetDedup(ctx);

            for (int i = 0; i < count; i++)
            {
                Collider col = OverlapBuffer[i];
                if (col.transform.root == ctx.SourceRoot) continue;

                IDamageable target = Hitbox.FindDamageable(col);
                if (target == null || !dedup.Add(target)) continue;

                if (_usePointHit)
                {
                    Vector3 point = col.ClosestPoint(ctx.Origin);
                    Hitbox.ApplyHit(col, info, point);
                }
                else
                {
                    target.TakeDamage(info);
                }

                ctx.HitAnything = true;
            }
        }

    }
}
