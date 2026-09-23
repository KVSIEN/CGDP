using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // Runs hit detection every physics tick during the Active phase of a melee
    // swing. Tracks which targets have already been hit this swing so each
    // entity is damaged at most once. Thrust and Sweep use SphereCasts for
    // point-of-contact resolution (hitbox regions, headshots). Slam uses
    // OverlapSphere for area damage.
    public class MeleeHitResolver
    {
        private static readonly RaycastHit[] CastBuffer = new RaycastHit[16];
        private static readonly Collider[]   OverlapBuffer = new Collider[16];

        private readonly HashSet<IDamageable> _hitThisSwing = new();

        private MeleeAttackStep _step;
        private DamageInfo      _info;
        private LayerMask       _mask;
        private Transform       _sourceRoot;
        private bool            _active;

        public bool HitAnything { get; private set; }

        public void Begin(MeleeAttackStep step, DamageInfo info, LayerMask mask, Transform sourceRoot)
        {
            _step       = step;
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

            switch (_step.HitShape)
            {
                case MeleeHitShape.Thrust:
                    TickThrust(origin, forward, debugDraw, debugDuration);
                    break;
                case MeleeHitShape.Sweep:
                    TickSweep(origin, forward, up, debugDraw, debugDuration);
                    break;
                case MeleeHitShape.Slam:
                    TickSlam(origin, forward, debugDraw, debugDuration);
                    break;
            }
        }

        public void End() => _active = false;

        private void TickThrust(Vector3 origin, Vector3 forward, bool debug, float debugDur)
        {
            int count = Physics.SphereCastNonAlloc(
                origin, _step.Radius, forward, CastBuffer,
                _step.Range, _mask, QueryTriggerInteraction.Ignore);

            ProcessCastHits(count);

            if (debug)
                Debug.DrawRay(origin, forward * _step.Range, _step.DebugColor, debugDur);
        }

        private void TickSweep(Vector3 origin, Vector3 forward, Vector3 up,
            bool debug, float debugDur)
        {
            float halfArc = _step.SweepArcDeg * 0.5f;
            int rays = Mathf.Max(_step.SweepRays, 2);

            for (int r = 0; r < rays; r++)
            {
                float t     = (float)r / (rays - 1);
                float angle = Mathf.Lerp(-halfArc, halfArc, t);
                Vector3 dir = Quaternion.AngleAxis(angle, up) * forward;

                int count = Physics.SphereCastNonAlloc(
                    origin, _step.Radius, dir, CastBuffer,
                    _step.Range, _mask, QueryTriggerInteraction.Ignore);

                ProcessCastHits(count);

                if (debug)
                    Debug.DrawRay(origin, dir * _step.Range, _step.DebugColor, debugDur);
            }
        }

        private void TickSlam(Vector3 origin, Vector3 forward, bool debug, float debugDur)
        {
            Vector3 center = origin + forward * _step.Range;

            int count = Physics.OverlapSphereNonAlloc(
                center, _step.Radius, OverlapBuffer,
                _mask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (OverlapBuffer[i].transform.root == _sourceRoot) continue;

                IDamageable target = Hitbox.FindDamageable(OverlapBuffer[i]);
                if (target != null && _hitThisSwing.Add(target))
                {
                    target.TakeDamage(_info);
                    HitAnything = true;
                }
            }

            if (debug)
                DebugDrawUtil.Sphere(center, _step.Radius, _step.DebugColor, debugDur);
        }

        private void ProcessCastHits(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Collider col = CastBuffer[i].collider;
                if (col.transform.root == _sourceRoot) continue;

                IDamageable target = Hitbox.FindDamageable(col);
                if (target == null || !_hitThisSwing.Add(target)) continue;

                Vector3 point = CastBuffer[i].point;
                // SphereCast reports zero when the sphere starts overlapping — use the
                // collider's closest surface point as a reasonable fallback.
                if (point == Vector3.zero)
                    point = col.ClosestPoint(_sourceRoot.position);

                Hitbox.ApplyHit(col, _info, point);
                HitAnything = true;
            }
        }

    }
}
