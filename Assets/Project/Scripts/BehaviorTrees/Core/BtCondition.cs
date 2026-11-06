using System;

namespace CGD.BehaviorTrees
{
    // A leaf that answers a question in one tick: Success when true, Failure when false.
    // Cheap enough to re-check every tick, which is what abort modes do (see AbortMode).
    [Serializable]
    public abstract class BtCondition : BtNode
    {
        protected sealed override BtStatus OnTick(BtContext context) =>
            Check(context) ? BtStatus.Success : BtStatus.Failure;

        protected abstract bool Check(BtContext context);

        internal override bool IsConditional => true;

        internal override bool CheckCondition(BtContext context) => Tick(context) == BtStatus.Success;
    }
}
