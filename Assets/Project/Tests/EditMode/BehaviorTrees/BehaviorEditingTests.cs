using System.Collections.Generic;
using NUnit.Framework;
using CGD.BehaviorTrees;
using CGD.Editor;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    // Behaviors: branches saved into their own tree and run through a Subtree, switched on
    // and off, and arranged in priority lists (the Behavior Composer's edits).
    public class BehaviorEditingTests
    {
        private BtTestTree _main;
        private BtTestTree _module;

        [SetUp]
        public void SetUp()
        {
            _main   = new BtTestTree();
            _module = new BtTestTree();
        }

        [TearDown]
        public void TearDown()
        {
            _main.Dispose();
            _module.Dispose();
        }

        [Test]
        public void ExtractingABranchLeavesASubtreeInItsPlace()
        {
            var list    = _main.Add(new Selector(), _main.Root);
            var branch  = _main.Add(new Sequence { Comment = "Pounce" }, list);
            var check   = _main.Add(new ScriptedCondition(), branch);
            var act     = _main.Add(new ScriptedLeaf(), branch);
            var fallback = _main.Add(new ScriptedLeaf(), list);

            Subtree subtree = BehaviorEditing.ExtractBranch(_main.Asset, branch, _module.Asset);

            Assert.AreEqual(new[] { subtree.Guid, fallback.Guid }, ChildGuids(list));
            Assert.AreSame(_module.Asset, subtree.Tree);
            Assert.AreEqual("Pounce", subtree.Comment);
            Assert.AreEqual("Pounce", _module.Root.Comment);
            Assert.AreEqual(new[] { branch.Guid }, ChildGuids(_module.Root));
            Assert.AreEqual(new[] { check.Guid, act.Guid }, ChildGuids(branch));
            Assert.IsNull(_main.Asset.FindNode(check.Guid));
            Assert.IsNotNull(_module.Asset.FindNode(act.Guid));
        }

        [Test]
        public void AnExtractedBranchStillRuns()
        {
            var list   = _main.Add(new Selector(), _main.Root);
            var branch = _main.Add(new Sequence(), list);
            var act    = _main.Add(new ScriptedLeaf { Result = BtStatus.Running }, branch);
            BehaviorEditing.ExtractBranch(_main.Asset, branch, _module.Asset);

            var tree = _main.Build();
            tree.Tick(0.1f);

            Assert.AreEqual(1, Copy(tree, _module.Asset, act).Ticks);
        }

        [Test]
        public void TheRootCannotBeExtracted()
        {
            Assert.IsNull(BehaviorEditing.ExtractBranch(_main.Asset, _main.Root, _module.Asset));
        }

        [Test]
        public void ASwitchedOffBehaviorIsSkippedAndNeverBuilt()
        {
            var inner = _module.Add(new ScriptedLeaf { Result = BtStatus.Running }, _module.Root);
            var list  = _main.Add(new Selector(), _main.Root);
            Subtree behavior = BehaviorEditing.AddBehavior(_main.Asset, list, _module.Asset);
            behavior.SetOn(false);
            var next = _main.Add(new ScriptedLeaf { Result = BtStatus.Running }, list);

            var tree = _main.Build();
            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Failure, Copy(tree, behavior).LastStatus);
            Assert.IsTrue(Copy(tree, next).IsRunning);
            Assert.IsFalse(tree.TryGetNode(_module.Asset, inner.Guid, out _));
        }

        [Test]
        public void AddingInsertsAtTheGivenPlace()
        {
            var list  = _main.Add(new Selector(), _main.Root);
            var first = _main.Add(new ScriptedLeaf(), list);
            var last  = _main.Add(new ScriptedLeaf(), list);

            Subtree added = BehaviorEditing.AddBehavior(_main.Asset, list, _module.Asset, 1);

            Assert.AreEqual(new[] { first.Guid, added.Guid, last.Guid }, ChildGuids(list));
        }

        [Test]
        public void MovingReordersTheList()
        {
            var list = _main.Add(new Selector(), _main.Root);
            var a = _main.Add(new ScriptedLeaf(), list);
            var b = _main.Add(new ScriptedLeaf(), list);
            var c = _main.Add(new ScriptedLeaf(), list);

            BehaviorEditing.Move(list, 2, 0);

            Assert.AreEqual(new[] { c.Guid, a.Guid, b.Guid }, ChildGuids(list));
        }

        [Test]
        public void RemovingABranchDeletesEverythingUnderIt()
        {
            var list   = _main.Add(new Selector(), _main.Root);
            var branch = _main.Add(new Sequence(), list);
            var leaf   = _main.Add(new ScriptedLeaf(), branch);

            BehaviorEditing.RemoveBranch(_main.Asset, branch);

            Assert.IsEmpty(list.ChildGuids);
            Assert.IsNull(_main.Asset.FindNode(leaf.Guid));
        }

        [Test]
        public void ListsAreTheSelectorsNamedByTheirBranch()
        {
            var top   = _main.Add(new Selector(), _main.Root);
            var fight = _main.Add(new Sequence { Comment = "Fight" }, top);
            _main.Add(new ScriptedCondition(), fight);
            var choices = _main.Add(new Selector(), fight);

            List<BehaviorEditing.BehaviorList> lists = BehaviorEditing.Lists(_main.Asset);

            Assert.AreEqual(2, lists.Count);
            Assert.AreSame(top, lists[0].Selector);
            Assert.AreSame(choices, lists[1].Selector);
            Assert.AreEqual("Fight", lists[1].Title);
            Assert.AreEqual(1, lists[1].Depth);
        }

        [Test]
        public void TheStarterTreeHasATopListAndAFightList()
        {
            using var starter = new BtTestTree();
            BehaviorEditing.BuildStarterTree(starter.Asset, null, null, _module.Asset);

            List<BehaviorEditing.BehaviorList> lists = BehaviorEditing.Lists(starter.Asset);

            Assert.AreEqual(2, lists.Count);
            Assert.AreEqual(2, lists[0].Selector.ChildGuids.Count);   // fight, routine
            Assert.IsEmpty(lists[1].Selector.ChildGuids);
            Assert.IsEmpty(BtTreeValidator.Validate(starter.Asset).FindAll(i => i.Severity == BtIssueSeverity.Error));
        }

        [Test]
        public void ASwitchedOffBehaviorOutsideASelectorIsFlagged()
        {
            var sequence = _main.Add(new Sequence(), _main.Root);
            var behavior = BehaviorEditing.AddBehavior(_main.Asset, sequence, _module.Asset);
            behavior.SetOn(false);

            List<BtIssue> issues = BtTreeValidator.Validate(_main.Asset);

            Assert.IsTrue(issues.Exists(i => i.NodeGuid == behavior.Guid && i.Severity == BtIssueSeverity.Warning));
        }

        private static string[] ChildGuids(BtParentNode node) => new List<string>(node.ChildGuids).ToArray();
    }
}
