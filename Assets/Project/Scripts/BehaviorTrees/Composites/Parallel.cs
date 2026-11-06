using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Runs all its children at the same time, e.g. keep moving while shooting. Children
    // that finish stop being ticked; once the policy is decided the rest are aborted.
    [Serializable]
    [BtNodeInfo("Composites/Parallel", "Runs every child at once. Finishes when the policy says so; the rest are aborted.")]
    public sealed class Parallel : BtComposite
    {
        [SerializeField] private ParallelPolicy _policy = ParallelPolicy.RequireAll;

        [NonSerialized] private BtStatus[] _results;

        public override string Summary => _policy == ParallelPolicy.RequireAll ? "Require all" : "Require one";

        protected override void OnInitialize(BtContext context) => _results = new BtStatus[Children.Count];

        protected override void OnEnter(BtContext context)
        {
            for (int i = 0; i < _results.Length; i++) _results[i] = BtStatus.Running;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            int succeeded = 0, failed = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                if (_results[i] == BtStatus.Running) _results[i] = Children[i].Tick(context);

                if (_results[i] == BtStatus.Success) succeeded++;
                else if (_results[i] == BtStatus.Failure) failed++;
            }

            BtStatus result = Decide(succeeded, failed);
            if (result != BtStatus.Running) OnAbort(context);
            return result;
        }

        private BtStatus Decide(int succeeded, int failed)
        {
            int all = Children.Count;
            if (_policy == ParallelPolicy.RequireAll)
            {
                if (failed > 0) return BtStatus.Failure;
                return succeeded == all ? BtStatus.Success : BtStatus.Running;
            }

            if (succeeded > 0) return BtStatus.Success;
            return failed == all ? BtStatus.Failure : BtStatus.Running;
        }
    }
}
