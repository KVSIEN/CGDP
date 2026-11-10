using System.Collections.Generic;
using NUnit.Framework;
using CGD.BehaviorTrees;
using CGD.Editor;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    public class ValidatorTests
    {
        private BtTestTree _t;

        [SetUp]    public void SetUp()    => _t = new BtTestTree();
        [TearDown] public void TearDown() => _t.Dispose();

        [Test]
        public void ACleanTreeHasNoIssues()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);
            _t.Add(new ScriptedLeaf(), sequence);

            Assert.IsEmpty(Validate());
        }

        [Test]
        public void AParentWithoutChildrenIsReported()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);

            Assert.IsTrue(HasIssue(sequence, BtIssueSeverity.Warning));
        }

        [Test]
        public void ANodeNotConnectedToTheRootIsReported()
        {
            _t.Add(new ScriptedLeaf(), _t.Root);
            var loose = new ScriptedLeaf();
            _t.Asset.AddNode(loose);

            Assert.IsTrue(HasIssue(loose, BtIssueSeverity.Warning));
        }

        [Test]
        public void ASubtreeWithoutATreeIsAnError()
        {
            var subtree = _t.Add(new Subtree(), _t.Root);

            Assert.IsTrue(HasIssue(subtree, BtIssueSeverity.Error));
        }

        [Test]
        public void ASubtreeThatLeadsBackToItsTreeIsAnError()
        {
            var subtree = _t.Add(Set(new Subtree(), "_tree", _t.Asset), _t.Root);

            Assert.IsTrue(HasIssue(subtree, BtIssueSeverity.Error));
        }

        [Test]
        public void AMotiveBranchThatCannotInterruptWarns()
        {
            var selector = _t.Add(new Selector(), _t.Root);
            var waiting  = _t.Add(new Sequence(), selector);
            _t.Add(new ScriptedCondition(), waiting);
            _t.Add(new Motive(), waiting);
            _t.Add(new ScriptedLeaf(), waiting);
            _t.Add(new ScriptedLeaf(), selector);

            Assert.IsTrue(HasIssue(waiting, BtIssueSeverity.Warning));

            Set(waiting, "_abort", AbortMode.LowerPriority);
            Assert.IsFalse(HasIssue(waiting, BtIssueSeverity.Warning));
        }

        [Test]
        public void AGateCountsAsALeadingCondition()
        {
            var selector = _t.Add(new Selector(), _t.Root);
            var gated    = _t.Add(Set(new Sequence(), "_abort", AbortMode.LowerPriority), selector);
            var cooldown = _t.Add(new Cooldown(), gated);
            _t.Add(new ScriptedLeaf(), cooldown);
            _t.Add(new ScriptedLeaf(), selector);

            Assert.IsFalse(HasIssue(gated, BtIssueSeverity.Warning));
        }

        [Test]
        public void ANeverEndingNodeWithNothingToInterruptItWarns()
        {
            var forever = _t.Add(Set(Set(new Repeat(), "_count", 0), "_stopOnFailure", false), _t.Root);
            _t.Add(new ScriptedLeaf(), forever);

            Assert.IsTrue(HasIssue(forever, BtIssueSeverity.Warning));
        }

        [Test]
        public void AnEarlierBranchThatAbortsLowerPriorityCanInterruptIt()
        {
            var selector = _t.Add(new Selector(), _t.Root);
            var urgent   = _t.Add(Set(new Sequence(), "_abort", AbortMode.LowerPriority), selector);
            _t.Add(new ScriptedCondition(), urgent);
            _t.Add(new ScriptedLeaf(), urgent);
            var forever = _t.Add(Set(Set(new Repeat(), "_count", 0), "_stopOnFailure", false), selector);
            _t.Add(new ScriptedLeaf(), forever);

            Assert.IsFalse(HasIssue(forever, BtIssueSeverity.Warning));
        }

        [Test]
        public void AnAbortModeWithNothingToCheckWarns()
        {
            var sequence = _t.Add(Set(new Sequence(), "_abort", AbortMode.LowerPriority), _t.Root);
            _t.Add(new ScriptedLeaf(), sequence);

            Assert.IsTrue(HasIssue(sequence, BtIssueSeverity.Warning));
        }

        [Test]
        public void AKeyReadButNeverWrittenWarnsUntilSomethingWritesIt()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);
            var check    = _t.Add(Set(new CheckFlag(), "_key", "NoTreeWritesThisKey_7f3"), sequence);
            Assert.IsTrue(HasIssue(check, BtIssueSeverity.Warning));

            _t.Add(Set(new SetFlag(), "_key", "NoTreeWritesThisKey_7f3"), sequence);
            Assert.IsFalse(HasIssue(check, BtIssueSeverity.Warning));
        }

        [Test]
        public void AnEmptyKeyWarnsExceptWhereEmptyMeansSomething()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);
            var check    = _t.Add(Set(new CheckFlag(), "_key", ""), sequence);
            var score    = _t.Add(Set(new Score(), "_key", ""), sequence);
            _t.Add(new ScriptedLeaf(), score);

            Assert.IsTrue(HasIssue(check, BtIssueSeverity.Warning));
            Assert.IsFalse(HasIssue(score, BtIssueSeverity.Warning));
        }

        private List<BtIssue> Validate() => BtTreeValidator.Validate(_t.Asset);

        private bool HasIssue(BtNode node, BtIssueSeverity severity)
        {
            foreach (BtIssue issue in Validate())
                if (issue.NodeGuid == node.Guid && issue.Severity == severity) return true;
            return false;
        }
    }
}
