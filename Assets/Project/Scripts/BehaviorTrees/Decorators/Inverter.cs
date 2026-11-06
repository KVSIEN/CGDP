using System;

namespace CGD.BehaviorTrees
{
    [Serializable]
    [BtNodeInfo("Decorators/Inverter", "Turns its child's success into failure and failure into success.")]
    public sealed class Inverter : BtDecorator
    {
        protected override BtStatus OnTick(BtContext context)
        {
            if (Child == null) return BtStatus.Failure;

            return Child.Tick(context) switch
            {
                BtStatus.Success => BtStatus.Failure,
                BtStatus.Failure => BtStatus.Success,
                _                => BtStatus.Running,
            };
        }
    }
}
