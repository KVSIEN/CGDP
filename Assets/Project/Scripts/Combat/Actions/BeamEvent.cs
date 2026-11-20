using System;
using UnityEngine;

namespace CGD.Combat
{
    [Serializable]
    public class BeamEvent : IActionEvent
    {
        private static readonly ShapeHits Shapes = new();

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
            var info = ctx.Prepare(new DamageInfo(
                _damagePerTick, _armorPenetration, _damageType,
                _criticalMultiplier, ctx.Source, _onHitEffects));

            // Each target once a tick, however many of its hitboxes the beam crosses.
            Shapes.Clear();
            Shapes.Cast(ctx.Origin, ctx.Forward, _radius, _range, ctx.HitMask, ctx.SourceRoot);
            foreach (ShapeHit hit in Shapes.Hits) Hitbox.ApplyHit(hit.Collider, info, hit.Point);
            if (Shapes.Hits.Count > 0) ctx.HitAnything = true;

            if (ctx.DebugDraw)
                Debug.DrawRay(ctx.Origin, ctx.Forward * _range, Color.magenta, ctx.DebugDuration);
        }
    }
}
