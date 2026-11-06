using NUnit.Framework;
using CGD.BehaviorTrees;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    public class TraceTests
    {
        private BtTestTree _t;

        [SetUp]    public void SetUp()    => _t = new BtTestTree();
        [TearDown] public void TearDown() => _t.Dispose();

        [Test]
        public void NothingIsRecordedUntilATraceIsStarted()
        {
            _t.Add(new ScriptedLeaf(), _t.Root);
            var tree = _t.Build();

            tree.Tick(0.1f);

            Assert.IsNull(tree.Trace);
        }

        [Test]
        public void RecordsWhatStartedAndHowItFinished()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);
            var leaf     = _t.Add(new ScriptedLeaf(), sequence);
            var tree = _t.Build();
            BtTrace trace = tree.StartTrace();

            tree.Tick(0.1f);

            Assert.AreEqual(1, Count(trace, BtTraceEvent.Started, Copy(tree, leaf)));
            Assert.AreEqual(1, Count(trace, BtTraceEvent.Succeeded, Copy(tree, leaf)));
            Assert.AreEqual(1, Count(trace, BtTraceEvent.Succeeded, Copy(tree, sequence)));
        }

        [Test]
        public void ConditionsAreOnlyLoggedWhenTheirAnswerChanges()
        {
            var condition = _t.Add(new ScriptedCondition(), _t.Root);
            var tree = _t.Build();
            BtTrace trace = tree.StartTrace();

            tree.Tick(0.1f);
            tree.Tick(0.1f);
            tree.Tick(0.1f);
            Assert.AreEqual(1, Count(trace, BtTraceEvent.Succeeded, Copy(tree, condition)));

            Copy(tree, condition).Holds = false;
            tree.Tick(0.1f);
            Assert.AreEqual(1, Count(trace, BtTraceEvent.Failed, Copy(tree, condition)));
        }

        [Test]
        public void AnInterruptionNamesWhatTookOver()
        {
            var selector  = _t.Add(new Selector(), _t.Root);
            var urgent    = _t.Add(Set(new Sequence(), "_abort", AbortMode.LowerPriority), selector);
            var condition = _t.Add(new ScriptedCondition { Holds = false }, urgent);
            _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, urgent);
            var routine = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, selector);
            var tree = _t.Build();
            BtTrace trace = tree.StartTrace();

            tree.Tick(0.1f);
            Copy(tree, condition).Holds = true;
            tree.Tick(0.1f);

            BtTraceEntry interruption = Find(trace, BtTraceEvent.Interrupted);
            Assert.AreSame(Copy(tree, routine), interruption.Node);
            Assert.AreSame(Copy(tree, urgent), interruption.Cause);
            Assert.AreEqual(0, Count(trace, BtTraceEvent.Aborted, Copy(tree, routine)), "the interruption already says it stopped");
        }

        private static int Count(BtTrace trace, BtTraceEvent kind, BtNode node)
        {
            int count = 0;
            for (int i = 0; i < trace.Count; i++)
                if (trace[i].Event == kind && trace[i].Node == node) count++;
            return count;
        }

        private static BtTraceEntry Find(BtTrace trace, BtTraceEvent kind)
        {
            for (int i = 0; i < trace.Count; i++)
                if (trace[i].Event == kind) return trace[i];
            Assert.Fail($"No {kind} entry");
            return default;
        }
    }
}
