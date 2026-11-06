using System;

namespace CGD.BehaviorTrees
{
    // Runs several children and combines their results (Sequence, Selector, Parallel…).
    [Serializable]
    public abstract class BtComposite : BtParentNode
    {
        internal sealed override int MaxChildren => int.MaxValue;
    }
}
