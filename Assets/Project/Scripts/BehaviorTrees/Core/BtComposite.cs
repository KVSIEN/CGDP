using System;

namespace CGD.BehaviorTrees
{
    // Runs several children and combines their results (Sequence, Selector, Parallel…).
    [Serializable]
    public abstract class BtComposite : BtParentNode
    {
        internal sealed override int MaxChildren => int.MaxValue;

        // Whether the run of conditional children it starts with holds: all of them
        // (requireAll), or at least one. False when it doesn't start with any.
        protected bool LeadingConditionsPass(BtContext context, bool requireAll)
        {
            bool any = false;
            foreach (BtNode child in Children)
            {
                if (!child.IsConditional) break;

                bool holds = child.CheckCondition(context);
                if (requireAll && !holds) return false;
                if (!requireAll && holds) return true;
                any = true;
            }
            return requireAll && any;
        }

        protected static string AbortSummary(AbortMode mode) => mode switch
        {
            AbortMode.Self          => "Aborts self",
            AbortMode.LowerPriority => "Aborts lower priority",
            AbortMode.Both          => "Aborts self + lower priority",
            _                       => null,
        };
    }
}
