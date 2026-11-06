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
    }
}
