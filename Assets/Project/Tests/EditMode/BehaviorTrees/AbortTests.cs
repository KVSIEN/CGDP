using NUnit.Framework;
using CGD.BehaviorTrees;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    public class AbortTests
    {
        private BtTestTree _t;

        [SetUp]    public void SetUp()    => _t = new BtTestTree();
        [TearDown] public void TearDown() => _t.Dispose();

        [Test]
        public void AbortSelfStopsASequenceWhenItsConditionBreaks()
        {
            var sequence  = _t.Add(Set(new Sequence(), "_abort", AbortMode.Self), _t.Root);
            var condition = _t.Add(new ScriptedCondition(), sequence);
            var action    = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, sequence);
            var tree = _t.Build();

            tree.Tick(0.1f);
            Assert.IsTrue(Copy(tree, action).IsRunning);

            Copy(tree, condition).Holds = false;
            tree.Tick(0.1f);

            Assert.IsFalse(Copy(tree, action).IsRunning);
            Assert.AreEqual(BtStatus.Failure, Copy(tree, sequence).LastStatus);
        }

        [Test]
        public void WithoutAnAbortModeTheRunningChildIsLeftAlone()
        {
            var sequence  = _t.Add(new Sequence(), _t.Root);
            var condition = _t.Add(new ScriptedCondition(), sequence);
            var action    = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, sequence);
            var tree = _t.Build();

            tree.Tick(0.1f);
            Copy(tree, condition).Holds = false;
            tree.Tick(0.1f);

            Assert.IsTrue(Copy(tree, action).IsRunning);
            Assert.AreEqual(1, Copy(tree, condition).Checks);
        }

        [Test]
        public void LowerPriorityAbortInterruptsARunningSibling()
        {
            var selector  = _t.Add(new Selector(), _t.Root);
            var urgent    = _t.Add(Set(new Sequence(), "_abort", AbortMode.LowerPriority), selector);
            var condition = _t.Add(new ScriptedCondition { Holds = false }, urgent);
            var response  = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, urgent);
            var routine   = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, selector);
            var tree = _t.Build();

            tree.Tick(0.1f);
            Assert.IsTrue(Copy(tree, routine).IsRunning);

            Copy(tree, condition).Holds = true;
            tree.Tick(0.1f);

            Assert.IsFalse(Copy(tree, routine).IsRunning);
            Assert.AreEqual(1, Copy(tree, routine).Exits);
            Assert.IsTrue(Copy(tree, response).IsRunning);
        }

        [Test]
        public void ACoolingDownGateDoesNotInterrupt()
        {
            var selector = _t.Add(new Selector(), _t.Root);
            var urgent   = _t.Add(Set(new Sequence(), "_abort", AbortMode.LowerPriority), selector);
            _t.Add(new ScriptedCondition(), urgent);
            var cooldown = _t.Add(Set(new Cooldown(), "_seconds", 5f), urgent);
            var response = _t.Add(new ScriptedLeaf(), cooldown);
            var routine  = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, selector);
            var tree = _t.Build();

            tree.Tick(0.1f);   // the urgent branch runs once; its cooldown starts
            tree.Tick(0.1f);   // cooling down: the routine runs
            tree.Tick(0.1f);   // still cooling: the routine keeps running

            Assert.AreEqual(1, Copy(tree, response).Ticks);
            Assert.AreEqual(2, Copy(tree, routine).Ticks);
            Assert.IsTrue(Copy(tree, routine).IsRunning);
        }

        [Test]
        public void AnInvertedConditionCanAbort()
        {
            var sequence  = _t.Add(Set(new Sequence(), "_abort", AbortMode.Self), _t.Root);
            var inverter  = _t.Add(new Inverter(), sequence);
            var condition = _t.Add(new ScriptedCondition { Holds = false }, inverter);
            var action    = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, sequence);
            var tree = _t.Build();

            tree.Tick(0.1f);
            Copy(tree, condition).Holds = true;
            tree.Tick(0.1f);

            Assert.IsFalse(Copy(tree, action).IsRunning);
        }
    }
}
