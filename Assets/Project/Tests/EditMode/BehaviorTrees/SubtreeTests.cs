using NUnit.Framework;
using CGD.BehaviorTrees;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    public class SubtreeTests
    {
        private BtTestTree _main;
        private BtTestTree _sub;

        [SetUp]
        public void SetUp()
        {
            _main = new BtTestTree();
            _sub  = new BtTestTree();
        }

        [TearDown]
        public void TearDown()
        {
            _main.Dispose();
            _sub.Dispose();
        }

        [Test]
        public void RunsTheOtherTreeAndReportsItsResult()
        {
            var leaf    = _sub.Add(new ScriptedLeaf { Result = BtStatus.Running }, _sub.Root);
            var subtree = _main.Add(Set(new Subtree(), "_tree", _sub.Asset), _main.Root);
            var tree = _main.Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Running, Copy(tree, subtree).LastStatus);
            Assert.AreEqual(1, Copy(tree, _sub.Asset, leaf).Ticks);
            Assert.IsTrue(tree.Uses(_sub.Asset));
        }

        [Test]
        public void SharesTheAgentsBlackboard()
        {
            _sub.Add(Set(new SetFlag(), "_key", "FromSubtree"), _sub.Root);
            _main.Add(Set(new Subtree(), "_tree", _sub.Asset), _main.Root);
            var tree = _main.Build();

            tree.Tick(0.1f);

            Assert.IsTrue(tree.Blackboard.GetBool("FromSubtree"));
        }

        [Test]
        public void PassesLowerPriorityAbortsThrough()
        {
            var urgent    = _sub.Add(Set(new Sequence(), "_abort", AbortMode.LowerPriority), _sub.Root);
            var condition = _sub.Add(new ScriptedCondition { Holds = false }, urgent);
            var response  = _sub.Add(new ScriptedLeaf { Result = BtStatus.Running }, urgent);

            var selector = _main.Add(new Selector(), _main.Root);
            _main.Add(Set(new Subtree(), "_tree", _sub.Asset), selector);
            var routine = _main.Add(new ScriptedLeaf { Result = BtStatus.Running }, selector);
            var tree = _main.Build();

            tree.Tick(0.1f);
            Copy(tree, _sub.Asset, condition).Holds = true;
            tree.Tick(0.1f);

            Assert.IsFalse(Copy(tree, routine).IsRunning);
            Assert.IsTrue(Copy(tree, _sub.Asset, response).IsRunning);
        }

        [Test]
        public void ASubtreeThatContainsItselfFailsInsteadOfRecursing()
        {
            var subtree = _main.Add(Set(new Subtree(), "_tree", _main.Asset), _main.Root);
            var tree = _main.Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Failure, Copy(tree, subtree).LastStatus);
        }
    }
}
