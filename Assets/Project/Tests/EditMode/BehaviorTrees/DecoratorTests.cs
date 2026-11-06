using NUnit.Framework;
using CGD.BehaviorTrees;
using CGD.Core;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    public class DecoratorTests
    {
        private BtTestTree _t;

        [SetUp]    public void SetUp()    => _t = new BtTestTree();
        [TearDown] public void TearDown() => _t.Dispose();

        [Test]
        public void InverterSwapsSuccessAndFailure()
        {
            var inverter = _t.Add(new Inverter(), _t.Root);
            _t.Add(new ScriptedLeaf { Result = BtStatus.Failure }, inverter);
            var tree = _t.Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Success, Copy(tree, inverter).LastStatus);
        }

        [Test]
        public void RepeatRunsItsChildTheSetNumberOfTimes()
        {
            var repeat = _t.Add(Set(new Repeat(), "_count", 3), _t.Root);
            var leaf   = _t.Add(new ScriptedLeaf(), repeat);
            var tree = _t.Build();

            tree.Tick(0.1f);
            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Running, Copy(tree, repeat).LastStatus);

            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Success, Copy(tree, repeat).LastStatus);
            Assert.AreEqual(3, Copy(tree, leaf).Ticks);
        }

        [Test]
        public void RepeatUntilRetriesUntilTheChildSucceeds()
        {
            var retry = _t.Add(Set(new RepeatUntil(), "_maxAttempts", 5), _t.Root);
            var leaf  = _t.Add(new ScriptedLeaf { Result = BtStatus.Failure }, retry);
            var tree = _t.Build();

            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Running, Copy(tree, retry).LastStatus);

            Copy(tree, leaf).Result = BtStatus.Success;
            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Success, Copy(tree, retry).LastStatus);
        }

        [Test]
        public void RepeatUntilGivesUpAfterItsAttempts()
        {
            var retry = _t.Add(Set(new RepeatUntil(), "_maxAttempts", 2), _t.Root);
            _t.Add(new ScriptedLeaf { Result = BtStatus.Failure }, retry);
            var tree = _t.Build();

            tree.Tick(0.1f);
            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Failure, Copy(tree, retry).LastStatus);
        }

        [Test]
        public void CooldownFailsWithoutRunningItsChildUntilItIsOver()
        {
            var cooldown = _t.Add(Set(new Cooldown(), "_seconds", 1f), _t.Root);
            var leaf     = _t.Add(new ScriptedLeaf(), cooldown);
            var tree = _t.Build();

            tree.Tick(0.1f);
            tree.Tick(0.5f);
            Assert.AreEqual(BtStatus.Failure, Copy(tree, cooldown).LastStatus);
            Assert.AreEqual(1, Copy(tree, leaf).Ticks);

            tree.Tick(0.6f);
            Assert.AreEqual(2, Copy(tree, leaf).Ticks);
        }

        [Test]
        public void ChanceOfOneAlwaysRunsAndZeroNever()
        {
            var always = _t.Add(Set(new Chance(), "_probability", 1f), _t.Root);
            var leaf   = _t.Add(new ScriptedLeaf(), always);
            var tree = _t.Build();
            tree.Tick(0.1f);
            Assert.AreEqual(1, Copy(tree, leaf).Ticks);

            using var other = new BtTestTree();
            var never     = other.Add(Set(new Chance(), "_probability", 0f), other.Root);
            var otherLeaf = other.Add(new ScriptedLeaf(), never);
            var otherTree = other.Build();
            otherTree.Tick(0.1f);
            Assert.AreEqual(0, Copy(otherTree, otherLeaf).Ticks);
            Assert.AreEqual(BtStatus.Failure, Copy(otherTree, never).LastStatus);
        }

        [Test]
        public void DelayWaitsBeforeRunningItsChild()
        {
            var delay = _t.Add(Set(new Delay(), "_seconds", new FloatRange(1f, 1f)), _t.Root);
            var leaf  = _t.Add(new ScriptedLeaf(), delay);
            var tree = _t.Build();

            tree.Tick(0.5f);
            Assert.AreEqual(0, Copy(tree, leaf).Ticks);

            tree.Tick(0.6f);
            Assert.AreEqual(1, Copy(tree, leaf).Ticks);
        }

        [Test]
        public void LimitRunsItsChildOnceUntilTheTreeResets()
        {
            var limit = _t.Add(Set(new Limit(), "_maxRuns", 1), _t.Root);
            var leaf  = _t.Add(new ScriptedLeaf(), limit);
            var tree = _t.Build();

            tree.Tick(0.1f);
            tree.Tick(0.1f);
            Assert.AreEqual(1, Copy(tree, leaf).Ticks);
            Assert.AreEqual(BtStatus.Failure, Copy(tree, limit).LastStatus);

            tree.Reset();
            tree.Tick(0.1f);
            Assert.AreEqual(2, Copy(tree, leaf).Ticks);
        }

        [Test]
        public void TimeLimitAbortsAChildThatRunsTooLong()
        {
            var limit = _t.Add(Set(new TimeLimit(), "_seconds", 1f), _t.Root);
            var leaf  = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, limit);
            var tree = _t.Build();

            tree.Tick(0.5f);   // starts at 0.5 s: runs out at 1.5 s
            tree.Tick(1.2f);

            Assert.AreEqual(BtStatus.Failure, Copy(tree, limit).LastStatus);
            Assert.IsFalse(Copy(tree, leaf).IsRunning);
        }
    }
}
