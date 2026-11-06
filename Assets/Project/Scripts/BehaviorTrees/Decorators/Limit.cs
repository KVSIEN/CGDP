using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Lets its child start at most a number of times, then fails without running it — a
    // once-per-life taunt, a single desperate charge. The count clears when the tree resets
    // (the agent respawns). A gate, so a spent limit never triggers an abort.
    [Serializable]
    [BtNodeInfo("Decorators/Limit", "Lets its child start at most N times (until the agent respawns), then fails.")]
    public sealed class Limit : BtDecorator
    {
        [SerializeField, Min(1)] private int _maxRuns = 1;

        [NonSerialized] private int  _runs;
        [NonSerialized] private bool _allowed;

        public override string Summary => _maxRuns == 1 ? "Once" : $"≤ {_maxRuns} times";

        internal override bool IsConditional => true;
        internal override bool CheckCondition(BtContext context) =>
            _runs < _maxRuns && (Child == null || !Child.IsConditional || Child.CheckCondition(context));

        protected override void OnEnter(BtContext context)
        {
            _allowed = _runs < _maxRuns;
            if (_allowed) _runs++;
        }

        protected override BtStatus OnTick(BtContext context) =>
            _allowed && Child != null ? Child.Tick(context) : BtStatus.Failure;

        protected override void OnReset() => _runs = 0;
    }
}
