using System;
using UnityEngine;
using CGD.Core;

namespace CGD.BehaviorTrees
{
    // Waits before running its child — a beat of hesitation before reacting, so an enemy
    // doesn't respond on the very frame something happens.
    [Serializable]
    [BtNodeInfo("Decorators/Delay", "Waits (random within the range), then runs its child.")]
    public sealed class Delay : BtDecorator
    {
        [SerializeField] private FloatRange _seconds = new(0.3f, 0.6f);

        [NonSerialized] private float _startsAt;

        public override string Summary => $"{_seconds.Min:0.##}–{_seconds.Max:0.##} s";

        protected override void OnEnter(BtContext context) => _startsAt = context.Time + _seconds.EvaluateClamped();

        protected override BtStatus OnTick(BtContext context)
        {
            if (context.Time < _startsAt) return BtStatus.Running;
            return Child != null ? Child.Tick(context) : BtStatus.Success;
        }
    }
}
