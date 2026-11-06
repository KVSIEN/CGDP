using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // After its child finishes, fails straight away for a while instead of running it —
    // keeps a special move (a charge, a grenade) from being used every time it's possible.
    [Serializable]
    [BtNodeInfo("Decorators/Cooldown", "After its child finishes, fails without running it until the cooldown is over.")]
    public sealed class Cooldown : BtDecorator
    {
        [SerializeField, Min(0f)] private float _seconds = 3f;
        [Tooltip("Only start the cooldown when the child succeeded")]
        [SerializeField] private bool _onlyAfterSuccess;

        [NonSerialized] private float _readyAt;

        public override string Summary => $"{_seconds:0.##} s";

        protected override BtStatus OnTick(BtContext context)
        {
            if (Child == null || context.Time < _readyAt) return BtStatus.Failure;

            BtStatus status = Child.Tick(context);
            if (status == BtStatus.Success || (status == BtStatus.Failure && !_onlyAfterSuccess))
                _readyAt = context.Time + _seconds;
            return status;
        }
    }
}
