using System;

namespace CGD.BehaviorTrees
{
    // Wraps one child and changes how it runs or what it reports (Inverter, Repeat,
    // Cooldown…). Without a child a decorator fails.
    [Serializable]
    public abstract class BtDecorator : BtParentNode
    {
        internal sealed override int MaxChildren => 1;

        protected BtNode Child => Children.Count > 0 ? Children[0] : null;
    }
}
