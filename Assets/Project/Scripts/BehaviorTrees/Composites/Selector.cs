using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Tries its children in order until one doesn't fail ("do the first of these that
    // works"). Fails only when every child has failed.
    // Reactive: every tick it re-tries the higher-priority children before the running one,
    // and switches to the first that no longer fails — the usual top of an AI tree, so
    // spotting the player interrupts a patrol.
    [Serializable]
    [BtNodeInfo("Composites/Selector", "Tries children left to right until one doesn't fail. Reactive: higher-priority children can interrupt.")]
    public sealed class Selector : BtComposite
    {
        [Tooltip("Re-try the children before the running one every tick; the first that no longer fails takes over")]
        [SerializeField] private bool _reactive;

        [NonSerialized] private int _current;

        public override string Summary => _reactive ? "Reactive" : null;

        protected override void OnEnter(BtContext context) => _current = 0;

        protected override BtStatus OnTick(BtContext context)
        {
            for (int i = _reactive ? 0 : _current; i < Children.Count; i++)
            {
                BtStatus status = Children[i].Tick(context);
                if (status == BtStatus.Failure) continue;

                // A higher-priority child took over: stop the lower one that was running.
                if (i < _current) Children[_current].Abort(context);
                _current = i;
                return status;
            }
            return BtStatus.Failure;
        }
    }
}
