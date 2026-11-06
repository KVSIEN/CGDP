using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Tries its children in order until one doesn't fail ("do the first of these that
    // works"). Fails only when every child has failed. Usually the top of a tree, most
    // urgent first: children whose Abort mode includes Lower Priority interrupt whatever is
    // running to their right as soon as their conditions hold — spotting the player stops
    // a patrol. Abort Self re-checks its own earlier conditional children the same way.
    [Serializable]
    [BtNodeInfo("Composites/Selector", "Tries children left to right until one doesn't fail. Higher branches with Lower Priority aborts interrupt.")]
    public sealed class Selector : BtComposite
    {
        [Tooltip("Self: switch to an earlier conditional child once it holds. Lower Priority: interrupt siblings to the right once any leading condition holds")]
        [SerializeField] private AbortMode _abort;

        [NonSerialized] private int _current;

        public override string Summary => AbortSummary(_abort);

        internal AbortMode AbortSetting => _abort;

        internal override bool AbortsLowerPriority => _abort is AbortMode.LowerPriority or AbortMode.Both;
        internal override bool PreconditionsPass(BtContext context) => LeadingConditionsPass(context, requireAll: false);

        private bool AbortsSelf => _abort is AbortMode.Self or AbortMode.Both;

        protected override void OnEnter(BtContext context) => _current = 0;

        protected override BtStatus OnTick(BtContext context)
        {
            int takeOver = HigherChildTakingOver(context);
            if (takeOver < _current)
            {
                if (Children[_current].IsRunning) context.Trace?.Interrupted(Children[_current], Children[takeOver]);
                Children[_current].Abort(context);
                _current = takeOver;
            }

            for (int i = _current; i < Children.Count; i++)
            {
                BtStatus status = Children[i].Tick(context);
                if (status == BtStatus.Failure) continue;

                _current = i;
                return status;
            }
            return BtStatus.Failure;
        }

        // The first child before the running one whose conditions now hold, or _current.
        private int HigherChildTakingOver(BtContext context)
        {
            for (int i = 0; i < _current; i++)
            {
                BtNode child = Children[i];
                if (child.AbortsLowerPriority && child.PreconditionsPass(context)) return i;
                if (AbortsSelf && child.IsConditional && child.CheckCondition(context)) return i;
            }
            return _current;
        }
    }
}
