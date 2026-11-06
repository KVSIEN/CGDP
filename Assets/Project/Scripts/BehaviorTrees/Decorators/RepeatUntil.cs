using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Runs its child again until it reports the wanted result — "retry until it works" or
    // "keep going until it fails". Gives up (fails) after the attempt limit. The child
    // restarts on the next tick, so it never loops within one frame.
    [Serializable]
    [BtNodeInfo("Decorators/Repeat Until", "Re-runs its child until it succeeds (retry) or fails, up to an attempt limit.")]
    public sealed class RepeatUntil : BtDecorator
    {
        [Tooltip("On: retry until the child succeeds. Off: repeat until it fails (then succeed)")]
        [SerializeField] private bool _untilSuccess = true;
        [Tooltip("Attempts before giving up and failing. 0 = no limit")]
        [SerializeField, Min(0)] private int _maxAttempts = 3;

        [NonSerialized] private int _attempts;

        public override string Summary =>
            (_untilSuccess ? "Until success" : "Until failure") + (_maxAttempts > 0 ? $", ≤ {_maxAttempts}" : "");

        protected override void OnEnter(BtContext context) => _attempts = 0;

        protected override BtStatus OnTick(BtContext context)
        {
            if (Child == null) return BtStatus.Failure;

            BtStatus status = Child.Tick(context);
            if (status == BtStatus.Running) return BtStatus.Running;

            BtStatus wanted = _untilSuccess ? BtStatus.Success : BtStatus.Failure;
            if (status == wanted) return BtStatus.Success;

            _attempts++;
            return _maxAttempts > 0 && _attempts >= _maxAttempts ? BtStatus.Failure : BtStatus.Running;
        }
    }
}
