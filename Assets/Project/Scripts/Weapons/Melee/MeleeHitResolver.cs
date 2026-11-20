using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Impacts;

namespace CGD.Weapons
{
    // Runs hit detection every physics tick during the Active phase of a melee
    // swing. Tracks which targets have already been hit this swing so each
    // entity is damaged at most once. Thrust and Sweep cast for point-of-contact
    // resolution (hitbox regions, headshots); Slam is area damage around the impact point.
    public class MeleeHitResolver
    {
        private static readonly ShapeHits Shapes = new();

        private readonly HashSet<IDamageable> _hitThisSwing = new();

        private MeleeAttackStep _step;
        private DamageInfo      _info;
        private LayerMask       _mask;
        private Transform       _sourceRoot;
        private float           _range;
        private bool            _active;

        public bool HitAnything { get; private set; }

        // `rangeScale` stretches the step's reach (reach perks and buffs).
        public void Begin(MeleeAttackStep step, DamageInfo info, LayerMask mask, Transform sourceRoot, float rangeScale = 1f)
        {
            _step       = step;
            _range      = step.Range * rangeScale;
            _info       = info;
            _mask       = mask;
            _sourceRoot = sourceRoot;
            _active     = true;
            HitAnything = false;
            _hitThisSwing.Clear();
        }

        public void Tick(Vector3 origin, Vector3 forward, Vector3 up, bool debugDraw, float debugDuration)
        {
            if (!_active) return;

            Shapes.Clear();
            switch (_step.HitShape)
            {
                case MeleeHitShape.Thrust:
                    Shapes.Cast(origin, forward, _step.Radius, _range, _mask, _sourceRoot, _hitThisSwing);
                    ApplyPointHits();
                    if (debugDraw) Debug.DrawRay(origin, forward * _range, _step.DebugColor, debugDuration);
                    break;

                case MeleeHitShape.Sweep:
                    Shapes.Fan(origin, forward, up, _step.SweepArcDeg, _step.SweepRays, _step.Radius, _range, _mask, _sourceRoot, _hitThisSwing);
                    ApplyPointHits();
                    if (debugDraw) DrawFan(origin, forward, up, debugDuration);
                    break;

                case MeleeHitShape.Slam:
                    Vector3 center = origin + forward * _range;
                    Shapes.Sphere(center, _step.Radius, origin, _mask, _sourceRoot, _hitThisSwing);
                    foreach (ShapeHit hit in Shapes.Hits) hit.Target.TakeDamage(_info);
                    if (Shapes.Hits.Count > 0) HitAnything = true;
                    if (debugDraw) DebugDrawUtil.Sphere(center, _step.Radius, _step.DebugColor, debugDuration);
                    break;
            }
        }

        public void End() => _active = false;

        private void ApplyPointHits()
        {
            foreach (ShapeHit hit in Shapes.Hits)
            {
                Hitbox.ApplyHit(hit.Collider, _info, hit.Point);
                if (hit.Normal.sqrMagnitude > 0.0001f) ImpactEvents.Report(hit.Point, hit.Normal.normalized, hit.Collider, ImpactKind.Melee);
                HitAnything = true;
            }
        }

        private void DrawFan(Vector3 origin, Vector3 forward, Vector3 up, float duration)
        {
            int rays = Mathf.Max(_step.SweepRays, 2);
            for (int i = 0; i < rays; i++)
                Debug.DrawRay(origin, ShapeHits.FanDirection(forward, up, _step.SweepArcDeg, rays, i) * _range, _step.DebugColor, duration);
        }
    }
}
