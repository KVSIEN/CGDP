using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Aborts its child and fails if it is still running after the limit — a safety net
    // for actions that could otherwise run forever (a path that never arrives).
    [Serializable]
    [BtNodeInfo("Decorators/Time Limit", "Aborts its child and fails if it runs longer than the limit.")]
    public sealed class TimeLimit : BtDecorator
    {
        [SerializeField, Min(0f)] private float _seconds = 5f;

        [NonSerialized] private float _endsAt;

        public override string Summary => $"{_seconds:0.##} s";

        protected override void OnEnter(BtContext context) => _endsAt = context.Time + _seconds;

        protected override BtStatus OnTick(BtContext context)
        {
            if (Child == null) return BtStatus.Failure;

            if (context.Time >= _endsAt)
            {
                if (Child.IsRunning) context.Trace?.Interrupted(Child, this);
                Child.Abort(context);
                return BtStatus.Failure;
            }
            return Child.Tick(context);
        }
    }
}
