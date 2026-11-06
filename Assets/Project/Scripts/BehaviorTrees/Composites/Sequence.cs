using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Runs its children in order until one fails ("do all of these"). Succeeds once every
    // child has succeeded.
    // Reactive: every tick it re-checks the children before the running one, and if one of
    // them now fails the running child is aborted. Put the conditions first:
    // [Target In Range] → [Melee Attack] stops attacking the moment the target steps away.
    [Serializable]
    [BtNodeInfo("Composites/Sequence", "Runs children left to right until one fails. Reactive: re-checks earlier children every tick.")]
    public sealed class Sequence : BtComposite
    {
        [Tooltip("Re-check the children before the running one every tick, aborting it if one now fails")]
        [SerializeField] private bool _reactive;

        [NonSerialized] private int _current;

        public override string Summary => _reactive ? "Reactive" : null;

        protected override void OnEnter(BtContext context) => _current = 0;

        protected override BtStatus OnTick(BtContext context)
        {
            for (int i = _reactive ? 0 : _current; i < Children.Count; i++)
            {
                BtStatus status = Children[i].Tick(context);
                if (status == BtStatus.Success) continue;

                // An earlier child took over (or failed): stop the one that was running.
                if (i < _current) Children[_current].Abort(context);
                _current = i;
                return status;
            }
            return BtStatus.Success;
        }
    }
}
