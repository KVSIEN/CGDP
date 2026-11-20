using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // Leaves a lingering patch of harmful ground (the shared DamageZone prefab) at a resolved
    // position, hitting the layers the action hits and tuned by its own Zone settings.
    [Serializable]
    public class SpawnZoneEvent : IActionEvent
    {
        [SerializeField] private int _startFrame;

        [Header("Zone")]
        [Tooltip("Pooled prefab with a DamageZone (Prefabs/VFX/DamageZone)")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private ActionSpace _space = ActionSpace.WorldOffset;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private ZoneSettings _zone = new();
        [Tooltip("Applied on every tick to everyone hostile inside")]
        [SerializeField] private StatusEffectApplication[] _onHitEffects;

        public int StartFrame => _startFrame;
        public int EndFrame => -1;

        public void OnEnter(ActionContext ctx)
        {
            if (_prefab == null) return;

            Vector3 position = ResolvePosition(ctx);
            Quaternion rot = Quaternion.LookRotation(ctx.Forward, ctx.Up);
            var go = PrefabPool.Spawn(_prefab, position, rot);

            if (go.TryGetComponent(out DamageZone zone))
                zone.Begin(_zone, _zone.Radius, ctx.Source, ctx.HitMask, _onHitEffects);
        }

        public void OnTick(ActionContext ctx) { }
        public void OnExit(ActionContext ctx) { }

        private Vector3 ResolvePosition(ActionContext ctx)
        {
            switch (_space)
            {
                case ActionSpace.CameraRelative:
                    Vector3 right = Vector3.Cross(ctx.Up, ctx.Forward).normalized;
                    return ctx.Origin
                         + right * _offset.x
                         + ctx.Up * _offset.y
                         + ctx.Forward * _offset.z;

                case ActionSpace.WorldOffset:
                    return (ctx.HasTarget ? ctx.TargetPoint : ctx.Origin) + _offset;

                case ActionSpace.WorldAbsolute:
                    return _offset;

                default:
                    return ctx.Origin;
            }
        }
    }
}
