using System;

namespace CGD.BehaviorTrees
{
    // Wraps one child and changes how it runs or what it reports (Inverter, Repeat,
    // Cooldown…). Without a child a decorator fails.
    [Serializable]
    public abstract class BtDecorator : BtParentNode
    {
        internal sealed override int MaxChildren => 1;

        // A gate (Cooldown, Limit, Director Permit) answers "may its child run now?" by itself, so
        // it counts as a condition for abort modes: a branch behind a closed gate never
        // interrupts only to fail. Gates also pass on their child's answer when it is a condition.
        internal virtual bool IsGate => false;

        internal override bool IsConditional => IsGate;

        protected BtNode Child => Children.Count > 0 ? Children[0] : null;
    }
}
