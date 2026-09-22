using System;
using UnityEngine;

namespace CGD.Combat
{
    public enum ForceTarget { Self, HitTargets }

    [Serializable]
    public class ForceEvent : IActionEvent
    {
        [SerializeField] private int _startFrame;

        [Header("Force")]
        [SerializeField] private ForceTarget _target = ForceTarget.HitTargets;
        [SerializeField] private float _force = 10f;
        [SerializeField] private Vector3 _direction = Vector3.forward;
        [SerializeField] private ForceMode _forceMode = ForceMode.Impulse;

        public int StartFrame => _startFrame;
        public int EndFrame => -1;

        public void OnEnter(ActionContext ctx)
        {
            Vector3 worldDir = ResolveDirection(ctx);

            if (_target == ForceTarget.Self)
            {
                if (ctx.SourceRoot != null && ctx.SourceRoot.TryGetComponent(out Rigidbody rb))
                    rb.AddForce(worldDir * _force, _forceMode);
                return;
            }

            foreach (IDamageable hit in ctx.SharedHits)
            {
                if (hit is Component comp)
                {
                    Rigidbody hitRb = comp.GetComponentInParent<Rigidbody>();
                    if (hitRb != null)
                        hitRb.AddForce(worldDir * _force, _forceMode);
                }
            }
        }

        public void OnTick(ActionContext ctx) { }
        public void OnExit(ActionContext ctx) { }

        private Vector3 ResolveDirection(ActionContext ctx)
        {
            Vector3 right = Vector3.Cross(ctx.Up, ctx.Forward).normalized;
            return (right * _direction.x + ctx.Up * _direction.y + ctx.Forward * _direction.z).normalized;
        }
    }
}
