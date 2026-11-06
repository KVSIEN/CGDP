using System;

namespace CGD.BehaviorTrees
{
    // The entry point every tree has exactly one of. When its branch finishes, the tree
    // starts again from the top on the next tick.
    [Serializable]
    [BtNodeInfo("Root", "Where the tree starts. Restarts its branch whenever it finishes.")]
    public sealed class BtRoot : BtParentNode
    {
        internal override int MaxChildren => 1;

        protected override BtStatus OnTick(BtContext context) =>
            Children.Count > 0 ? Children[0].Tick(context) : BtStatus.Failure;
    }
}
