using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Runs its children in order until one fails ("do all of these"). Succeeds once every
    // child has succeeded. Start it with conditions so it only runs when they hold:
    // [Has Target → Fight]. Abort Self keeps re-checking those conditions while a later
    // child runs; Lower Priority lets them interrupt a sibling to its right (see AbortMode).
    [Serializable]
    [BtNodeInfo("Composites/Sequence", "Runs children left to right until one fails. Abort modes re-check its leading conditions.")]
    public sealed class Sequence : BtComposite
    {
        [Tooltip("Self: stop when an earlier condition no longer holds. Lower Priority: interrupt siblings to the right once its leading conditions hold")]
        [SerializeField] private AbortMode _abort;

        [NonSerialized] private int _current;

        public override string Summary => AbortSummary(_abort);

        internal AbortMode AbortSetting => _abort;

        internal override bool AbortsLowerPriority => _abort is AbortMode.LowerPriority or AbortMode.Both;
        internal override bool PreconditionsPass(BtContext context) => LeadingConditionsPass(context, requireAll: true);

        private bool AbortsSelf => _abort is AbortMode.Self or AbortMode.Both;

        protected override void OnEnter(BtContext context) => _current = 0;

        protected override BtStatus OnTick(BtContext context)
        {
            int broken = AbortsSelf ? BrokenConditionBefore(_current, context) : -1;
            if (broken >= 0)
            {
                context.Trace?.Interrupted(Children[_current], Children[broken]);
                Children[_current].Abort(context);
                return BtStatus.Failure;
            }

            for (int i = _current; i < Children.Count; i++)
            {
                BtStatus status = Children[i].Tick(context);
                if (status == BtStatus.Success) continue;

                _current = i;
                return status;
            }
            return BtStatus.Success;
        }

        // The first conditional child before index that no longer holds, or -1.
        private int BrokenConditionBefore(int index, BtContext context)
        {
            for (int i = 0; i < index; i++)
                if (Children[i].IsConditional && !Children[i].CheckCondition(context)) return i;
            return -1;
        }
    }
}
