using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Reports a fixed result once its child finishes, whatever the child reported — e.g. an
    // optional step in a Sequence that mustn't stop it when it fails.
    [Serializable]
    [BtNodeInfo("Decorators/Force Result", "Once its child finishes, reports the chosen result instead.")]
    public sealed class ForceResult : BtDecorator
    {
        [SerializeField] private bool _succeed = true;

        public override string Summary => _succeed ? "Always succeed" : "Always fail";

        protected override BtStatus OnTick(BtContext context)
        {
            BtStatus status = Child != null ? Child.Tick(context) : BtStatus.Success;
            if (status == BtStatus.Running) return BtStatus.Running;
            return _succeed ? BtStatus.Success : BtStatus.Failure;
        }
    }
}
