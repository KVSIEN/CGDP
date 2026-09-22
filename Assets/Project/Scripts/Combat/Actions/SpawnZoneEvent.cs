using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    [Serializable]
    public class SpawnZoneEvent : IActionEvent
    {
        [SerializeField] private int _startFrame;

        [Header("Zone")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private ActionSpace _space = ActionSpace.WorldOffset;
        [SerializeField] private Vector3 _offset;

        public int StartFrame => _startFrame;
        public int EndFrame => -1;

        public void OnEnter(ActionContext ctx)
        {
            if (_prefab == null) return;

            Vector3 position = ResolvePosition(ctx);
            Quaternion rot = Quaternion.LookRotation(ctx.Forward, ctx.Up);
            var go = PrefabPool.Spawn(_prefab, position, rot);

            if (go.TryGetComponent(out PersistentZone zone))
                zone.Init(ctx.Source, ctx.HitMask);
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
