using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    [Serializable]
    public class ShapeHitEvent : IActionEvent
    {
        private static readonly ShapeHits Shapes = new();

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
            HashSet<IDamageable> dedup = _useSharedDedup ? ctx.SharedHits : ctx.EventHits[ctx.CurrentEventIndex];

            Shapes.Clear();
            switch (_shape)
            {
                case HitShape.SphereCast:
                    Shapes.Cast(origin, forward, _radius, _range, ctx.HitMask, ctx.SourceRoot, dedup);
                    if (ctx.DebugDraw) Debug.DrawRay(origin, forward * _range, Color.red, ctx.DebugDuration);
                    break;

                case HitShape.Arc:
                    Shapes.Fan(origin, forward, up, _arcDeg, _rayCount, _radius, _range, ctx.HitMask, ctx.SourceRoot, dedup);
                    if (ctx.DebugDraw) DrawArc(ctx, origin, forward, up);
                    break;

                case HitShape.Sphere:
                    Vector3 sphereCenter = origin + forward * _range;
                    Shapes.Sphere(sphereCenter, _radius, ctx.Origin, ctx.HitMask, ctx.SourceRoot, dedup);
                    if (ctx.DebugDraw) DebugDrawUtil.Sphere(sphereCenter, _radius, Color.cyan, ctx.DebugDuration);
                    break;

                case HitShape.Box:
                    Vector3 boxCenter = origin + forward * _range;
                    Quaternion orientation = Quaternion.LookRotation(forward, ctx.Up);
                    Shapes.Box(boxCenter, _halfExtents, orientation, ctx.Origin, ctx.HitMask, ctx.SourceRoot, dedup);
                    if (ctx.DebugDraw) DebugDrawUtil.Box(boxCenter, _halfExtents, orientation, Color.green, ctx.DebugDuration);
                    break;
            }

            ApplyHits(ctx);
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

        private void ApplyHits(ActionContext ctx)
        {
            if (Shapes.Hits.Count == 0) return;

            DamageInfo info = ctx.Prepare(new DamageInfo(
                _damage, _armorPenetration, _damageType,
                _criticalMultiplier, ctx.Source, _onHitEffects));

            foreach (ShapeHit hit in Shapes.Hits)
            {
                if (_usePointHit) Hitbox.ApplyHit(hit.Collider, info, hit.Point);
                else              hit.Target.TakeDamage(info);
            }
            ctx.HitAnything = true;
        }

        private void DrawArc(ActionContext ctx, Vector3 origin, Vector3 forward, Vector3 up)
        {
            int rays = Mathf.Max(_rayCount, 2);
            for (int i = 0; i < rays; i++)
                Debug.DrawRay(origin, ShapeHits.FanDirection(forward, up, _arcDeg, rays, i) * _range, Color.yellow, ctx.DebugDuration);
        }
    }
}
