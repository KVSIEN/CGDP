using System;

namespace CGD.BehaviorTrees
{
    [Serializable]
    [BtNodeInfo("Decorators/Inverter", "Turns its child's success into failure and failure into success.")]
    public sealed class Inverter : BtDecorator
    {
        // An inverted condition is still a condition, so abort modes can re-check it.
        internal override bool IsConditional => Child != null && Child.IsConditional;
        internal override bool CheckCondition(BtContext context) => !Child.CheckCondition(context);

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
