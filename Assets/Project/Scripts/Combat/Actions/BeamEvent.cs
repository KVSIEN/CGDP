using System;
using UnityEngine;

namespace CGD.Combat
{
    [Serializable]
    public class BeamEvent : IActionEvent
    {
        private static readonly RaycastHit[] CastBuffer = new RaycastHit[16];

        [SerializeField] private int _startFrame;
        [SerializeField] private int _endFrame = 10;

        [Header("Beam")]
        [SerializeField] private float _range = 20f;
        [SerializeField] private float _damagePerTick = 5f;
        [Range(0f, 1f)]
        [SerializeField] private float _armorPenetration;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private float _criticalMultiplier = 1f;
        [Min(0f)]
        [SerializeField] private float _radius;
        [SerializeField] private StatusEffectApplication[] _onHitEffects;

        public int StartFrame => _startFrame;
        public int EndFrame => _endFrame;

        public void OnEnter(ActionContext ctx) { }
        public void OnExit(ActionContext ctx) { }

        public void OnTick(ActionContext ctx)
        {
            var info = new DamageInfo(
                _damagePerTick, _armorPenetration, _damageType,
                _criticalMultiplier, ctx.Source, _onHitEffects);

            int count;
            if (_radius > 0f)
            {
                count = Physics.SphereCastNonAlloc(
                    ctx.Origin, _radius, ctx.Forward, CastBuffer,
                    _range, ctx.HitMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                count = Physics.RaycastNonAlloc(
                    ctx.Origin, ctx.Forward, CastBuffer,
                    _range, ctx.HitMask, QueryTriggerInteraction.Ignore);
            }

            for (int i = 0; i < count; i++)
            {
                Collider col = CastBuffer[i].collider;
                if (col.transform.root == ctx.SourceRoot) continue;

                Vector3 point = CastBuffer[i].point;
                if (point == Vector3.zero)
                    point = col.ClosestPoint(ctx.SourceRoot.position);

                Hitbox.ApplyHit(col, info, point);
                ctx.HitAnything = true;
            }

            if (ctx.DebugDraw)
                Debug.DrawRay(ctx.Origin, ctx.Forward * _range, Color.magenta, ctx.DebugDuration);
        }
    }
}
