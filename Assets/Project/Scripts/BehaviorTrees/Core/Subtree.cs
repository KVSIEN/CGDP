using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Runs another behavior tree asset in its place — the same agent, the same blackboard —
    // so a branch used by several trees (regrouping, an idle routine) is built and fixed once.
    // Reports whatever the subtree's branch reports. Conditional aborts pass through: a
    // subtree whose branch aborts lower priority does so from here too.
    // A subtree that would end up containing itself is refused and fails.
    //
    // Switched off, it is skipped: it fails at once and never builds its tree, so a behavior
    // can be turned off for one enemy type and back on without rewiring anything (in a
    // priority Selector, failing simply hands the turn to the next behavior).
    [Serializable]
    [BtNodeInfo("Subtree", "Runs another behavior tree asset here, sharing this agent and its blackboard.")]
    public sealed class Subtree : BtNode
    {
        [Tooltip("The tree to run here (double-click the node to open it)")]
        [SerializeField] private BehaviorTreeAsset _tree;
        [Tooltip("Skips this behavior (it fails at once) without removing it from the tree")]
        [SerializeField] private bool _off;

        [NonSerialized] private BtRoot _root;

        public BehaviorTreeAsset Tree => _tree;
        public bool IsOn => !_off;

        public override string Summary => (_off ? "OFF · " : "") + (_tree != null ? _tree.name : "(no tree)");

        // The subtree's own branch (what runs in this node's place).
        internal BtNode Branch => _root?.Branch;

        // --- Authoring (editor) ----------------------------------------------------

        internal void SetTree(BehaviorTreeAsset tree) => _tree = tree;
        internal void SetOn(bool on) => _off = !on;

        internal override bool IsConditional => Branch != null && Branch.IsConditional;
        internal override bool CheckCondition(BtContext context) => Branch.CheckCondition(context);
        internal override bool AbortsLowerPriority => Branch != null && Branch.AbortsLowerPriority;
        internal override bool PreconditionsPass(BtContext context) => Branch.PreconditionsPass(context);

        protected override void OnInitialize(BtContext context)
        {
            if (!_off) _root = context.Tree.Instantiate(_tree);
        }

        protected override BtStatus OnTick(BtContext context) => _root != null ? _root.Tick(context) : BtStatus.Failure;

        protected override void OnAbort(BtContext context) => _root?.Abort(context);
    }
}
