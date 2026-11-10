using NUnit.Framework;
using CGD.BehaviorTrees;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    public class CompositeTests
    {
        private BtTestTree _t;

        [SetUp]    public void SetUp()    => _t = new BtTestTree();
        [TearDown] public void TearDown() => _t.Dispose();

        [Test]
        public void SequenceSucceedsWhenEveryChildSucceeds()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);
            var a = _t.Add(new ScriptedLeaf(), sequence);
            var b = _t.Add(new ScriptedLeaf(), sequence);
            var tree = _t.Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Success, Copy(tree, sequence).LastStatus);
            Assert.AreEqual(1, Copy(tree, a).Ticks);
            Assert.AreEqual(1, Copy(tree, b).Ticks);
        }

        [Test]
        public void SequenceStopsAtTheFirstFailure()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);
            _t.Add(new ScriptedLeaf { Result = BtStatus.Failure }, sequence);
            var later = _t.Add(new ScriptedLeaf(), sequence);
            var tree = _t.Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Failure, Copy(tree, sequence).LastStatus);
            Assert.AreEqual(0, Copy(tree, later).Ticks);
        }

        [Test]
        public void SelectorRunsTheFirstChildThatDoesNotFail()
        {
            var selector = _t.Add(new Selector(), _t.Root);
            _t.Add(new ScriptedLeaf { Result = BtStatus.Failure }, selector);
            var second = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, selector);
            var third  = _t.Add(new ScriptedLeaf(), selector);
            var tree = _t.Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Running, Copy(tree, selector).LastStatus);
            Assert.AreEqual(1, Copy(tree, second).Ticks);
            Assert.AreEqual(0, Copy(tree, third).Ticks);
        }

        [Test]
        public void ParallelRequireOneSucceedsAndAbortsTheRest()
        {
            var parallel = _t.Add(Set(new Parallel(), "_policy", ParallelPolicy.RequireOne), _t.Root);
            var slow = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, parallel);
            _t.Add(new ScriptedLeaf(), parallel);
            var tree = _t.Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Success, Copy(tree, parallel).LastStatus);
            Assert.IsFalse(Copy(tree, slow).IsRunning);
        }

        [Test]
        public void WeightedRandomSelectorNeverRunsAZeroWeightChild()
        {
            var selector = _t.Add(Set(new WeightedRandomSelector(), "_weights", new[] { 0f, 1f }), _t.Root);
            var never  = _t.Add(new ScriptedLeaf(), selector);
            var always = _t.Add(new ScriptedLeaf(), selector);
            var tree = _t.Build();

            for (int i = 0; i < 20; i++) tree.Tick(0.1f);

            Assert.AreEqual(0, Copy(tree, never).Ticks);
            Assert.AreEqual(20, Copy(tree, always).Ticks);
        }

        [Test]
        public void UtilitySelectorRunsTheBestScoringChild()
        {
            var utility = _t.Add(Set(new UtilitySelector(), "_randomness", 0f), _t.Root);
            var low  = FixedScore(utility, 0.2f);
            var high = FixedScore(utility, 0.8f);
            var lowLeaf  = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, low);
            var highLeaf = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, high);
            var tree = _t.Build();

            tree.Tick(0.1f);

            Assert.AreEqual(0, Copy(tree, lowLeaf).Ticks);
            Assert.AreEqual(1, Copy(tree, highLeaf).Ticks);
        }

        [Test]
        public void UtilitySelectorSwitchesWhenAnotherChildClearlyWins()
        {
            var utility = _t.Add(new UtilitySelector(), _t.Root);
            Set(utility, "_randomness", 0f);
            Set(utility, "_reconsiderEvery", 0.1f);
            var keyed = _t.Add(Set(Set(new Score(), "_key", "Want"), "_weight", 1f), utility);
            var fixedHalf = FixedScore(utility, 0.5f);
            var wanted  = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, keyed);
            var default_ = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, fixedHalf);
            var tree = _t.Build();

            tree.Blackboard.SetFloat("Want", 0f);
            tree.Tick(0.05f);
            Assert.IsTrue(Copy(tree, default_).IsRunning);

            tree.Blackboard.SetFloat("Want", 1f);
            tree.Tick(0.2f);

            Assert.IsFalse(Copy(tree, default_).IsRunning);
            Assert.IsTrue(Copy(tree, wanted).IsRunning);
        }

        [Test]
        public void UtilitySelectorKeepsRunningWhenTheBetterChildCannotStart()
        {
            var utility = _t.Add(new UtilitySelector(), _t.Root);
            Set(utility, "_randomness", 0f);
            Set(utility, "_reconsiderEvery", 0.1f);
            var fallback = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, FixedScore(utility, 0.2f));
            var blocked  = _t.Add(new Sequence(), FixedScore(utility, 0.9f));
            _t.Add(new ScriptedCondition { Holds = false }, blocked);
            _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, blocked);
            var tree = _t.Build();

            tree.Tick(0.05f);
            tree.Tick(0.2f);
            tree.Tick(0.2f);

            Assert.IsTrue(Copy(tree, fallback).IsRunning);
            Assert.AreEqual(1, Copy(tree, fallback).Enters);
        }

        private Score FixedScore(BtNode parent, float weight) =>
            _t.Add(Set(Set(new Score(), "_key", ""), "_weight", weight), parent);
    }
}
