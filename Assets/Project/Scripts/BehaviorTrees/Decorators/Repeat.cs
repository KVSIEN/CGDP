using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Runs its child again each time it finishes: a set number of times, or forever.
    // The child restarts on the next tick, so a repeat never loops within one frame.
    [Serializable]
    [BtNodeInfo("Decorators/Repeat", "Runs its child again each time it finishes, a set number of times or forever.")]
    public sealed class Repeat : BtDecorator
    {
        [Tooltip("How many runs before succeeding. 0 = forever")]
        [SerializeField, Min(0)] private int _count;
        [Tooltip("Stop and fail as soon as the child fails")]
        [SerializeField] private bool _stopOnFailure = true;

        [NonSerialized] private int _runs;

        public override string Summary => _count > 0 ? $"× {_count}" : "Forever";

        protected override void OnEnter(BtContext context) => _runs = 0;

        protected override BtStatus OnTick(BtContext context)
        {
            if (Child == null) return BtStatus.Failure;

            BtStatus status = Child.Tick(context);
            if (status == BtStatus.Running) return BtStatus.Running;
            if (status == BtStatus.Failure && _stopOnFailure) return BtStatus.Failure;

            _runs++;
            return _count > 0 && _runs >= _count ? BtStatus.Success : BtStatus.Running;
        }
    }
}
