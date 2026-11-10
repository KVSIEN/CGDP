using NUnit.Framework;
using CGD.BehaviorTrees;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    public class BlackboardNodeTests
    {
        private BtTestTree _t;

        [SetUp]    public void SetUp()    => _t = new BtTestTree();
        [TearDown] public void TearDown() => _t.Dispose();

        [Test]
        public void FlagsRoundTrip()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);
            _t.Add(Set(new SetFlag(), "_key", "Seen"), sequence);
            _t.Add(Set(new CheckFlag(), "_key", "Seen"), sequence);
            var tree = _t.Build();

            tree.Tick(0.1f);

            Assert.IsTrue(tree.Blackboard.GetBool("Seen"));
            Assert.AreEqual(BtStatus.Success, Copy(tree, sequence).LastStatus);
        }

        [Test]
        public void SetNumberAddsToAMissingKeyFromZero()
        {
            var add = Set(Set(new SetNumber(), "_key", "Hits"), "_operation", NumberOperation.Add);
            _t.Add(Set(add, "_value", 2f), _t.Root);
            var tree = _t.Build();

            tree.Tick(0.1f);
            tree.Tick(0.1f);

            Assert.AreEqual(4f, tree.Blackboard.GetFloat("Hits"), 0.0001f);
        }

        [Test]
        public void CompareNumberChecksTheValue()
        {
            var compare = Set(Set(new CompareNumber(), "_key", "TargetDistance"), "_comparison", NumberComparison.Less);
            _t.Add(Set(compare, "_value", 6f), _t.Root);
            var tree = _t.Build();

            tree.Blackboard.SetFloat("TargetDistance", 4f);
            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Success, Copy(tree, compare).LastStatus);

            tree.Blackboard.SetFloat("TargetDistance", 8f);
            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Failure, Copy(tree, compare).LastStatus);
        }

        [Test]
        public void CompareNumberGivesTheMissingAnswerWithoutAKey()
        {
            var compare = _t.Add(Set(new CompareNumber(), "_ifMissing", true), _t.Root);
            var tree = _t.Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Success, Copy(tree, compare).LastStatus);
        }

        [Test]
        public void ClearKeyRemovesWhatHasKeyFinds()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);
            var has      = _t.Add(Set(new HasKey(), "_key", "Spot"), sequence);
            _t.Add(Set(new ClearKey(), "_key", "Spot"), sequence);
            var tree = _t.Build();

            tree.Blackboard.SetFloat("Spot", 1f);
            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Success, Copy(tree, has).LastStatus);
            Assert.IsFalse(tree.Blackboard.Has("Spot"));

            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Failure, Copy(tree, has).LastStatus);
        }

        [Test]
        public void MotiveActsOnceItsReasonsAddUp()
        {
            var motive = Set(new Motive(), "_patience", 0f);
            Set(motive, "_reasons", new[] { Reason("Health", from: 1f, to: 0f, weight: 1f) });
            _t.Add(motive, _t.Root);
            var tree = _t.Build();

            tree.Blackboard.SetFloat("Health", 0.3f);
            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Failure, Copy(tree, motive).LastStatus);

            tree.Blackboard.SetFloat("Health", 0f);
            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Success, Copy(tree, motive).LastStatus);
        }

        [Test]
        public void MotiveWithoutAReasonActsOnceItsPatienceRunsOut()
        {
            var motive = _t.Add(Set(new Motive(), "_patience", 1f), _t.Root);
            var tree = _t.Build();

            for (int i = 0; i < 3; i++)
            {
                tree.Tick(0.4f);
                Assert.AreEqual(BtStatus.Failure, Copy(tree, motive).LastStatus);
            }
            tree.Tick(0.4f);
            Assert.AreEqual(BtStatus.Success, Copy(tree, motive).LastStatus);
        }

        [Test]
        public void MotivePatienceInterruptsWhatRunsToItsRight()
        {
            // The Selector asks the Motive whether its branch may interrupt, then the branch asks it
            // again as it runs: both in one tick, and both must say yes.
            var selector = _t.Add(new Selector(), _t.Root);
            var urgent   = _t.Add(Set(new Sequence(), "_abort", AbortMode.LowerPriority), selector);
            _t.Add(Set(new Motive(), "_patience", 1f), urgent);
            var cast     = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, urgent);
            var routine  = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, selector);
            var tree = _t.Build();

            for (int i = 0; i < 3; i++) tree.Tick(0.4f);
            Assert.IsTrue(Copy(tree, routine).IsRunning);
            Assert.AreEqual(0, Copy(tree, cast).Enters);

            tree.Tick(0.4f);
            Assert.AreEqual(1, Copy(tree, cast).Enters);
            Assert.IsFalse(Copy(tree, routine).IsRunning);
        }

        [Test]
        public void MotivePatienceSurvivesAskingAboutOnceASecond()
        {
            // A Utility Selector re-scores every second; patience must keep building across that gap.
            var motive = _t.Add(Set(new Motive(), "_patience", 3f), _t.Root);
            var tree = _t.Build();

            for (int i = 0; i < 3; i++)
            {
                tree.Tick(1.2f);
                Assert.AreEqual(BtStatus.Failure, Copy(tree, motive).LastStatus);
            }
            tree.Tick(1.2f);
            Assert.AreEqual(BtStatus.Success, Copy(tree, motive).LastStatus);
        }

        private static MotiveReason Reason(string key, float from, float to, float weight)
        {
            var reason = new MotiveReason();
            SetField(reason, "_key", key);
            SetField(reason, "_from", from);
            SetField(reason, "_to", to);
            SetField(reason, "_weight", weight);
            return reason;
        }

        private static void SetField(object target, string field, object value) =>
            target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                  .SetValue(target, value);
    }
}
