using System.Collections.Generic;
using NUnit.Framework;
using CGD.BehaviorTrees;
using static CGD.Tests.BtTestTree;

namespace CGD.Tests
{
    // What debug views read from a running tree: the nodes running now, shared trace
    // recording, and node names.
    public class DebugViewTests
    {
        private BtTestTree _t;
        private readonly List<(BtNode Node, int Depth)> _running = new();

        [SetUp]    public void SetUp()    => _t = new BtTestTree();
        [TearDown] public void TearDown() => _t.Dispose();

        [Test]
        public void RunningNodesListTheBranchFromTheTopDown()
        {
            var sequence = _t.Add(new Sequence(), _t.Root);
            _t.Add(new ScriptedLeaf(), sequence);
            var busy = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, sequence);
            var tree = _t.Build();

            tree.Tick(0.1f);
            tree.CollectRunning(_running);

            Assert.AreEqual(2, _running.Count);   // the root itself is left out
            Assert.AreSame(Copy(tree, sequence), _running[0].Node);
            Assert.AreEqual(0, _running[0].Depth);
            Assert.AreSame(Copy(tree, busy), _running[1].Node);
            Assert.AreEqual(1, _running[1].Depth);
        }

        [Test]
        public void EveryRunningBranchOfAParallelAppears()
        {
            var parallel = _t.Add(new Parallel(), _t.Root);
            var a = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, parallel);
            var b = _t.Add(new ScriptedLeaf { Result = BtStatus.Running }, parallel);
            var tree = _t.Build();

            tree.Tick(0.1f);
            tree.CollectRunning(_running);

            Assert.AreEqual(3, _running.Count);
            Assert.AreSame(Copy(tree, a), _running[1].Node);
            Assert.AreSame(Copy(tree, b), _running[2].Node);
            Assert.AreEqual(1, _running[2].Depth);
        }

        [Test]
        public void NothingIsRunningOnceTheBranchFinished()
        {
            _t.Add(new ScriptedLeaf(), _t.Root);
            var tree = _t.Build();

            tree.Tick(0.1f);
            tree.CollectRunning(_running);

            Assert.AreEqual(0, _running.Count);
        }

        [Test]
        public void ASubtreesNodesContinueUnderItsNode()
        {
            using var sub = new BtTestTree();
            var leaf    = sub.Add(new ScriptedLeaf { Result = BtStatus.Running }, sub.Root);
            var subtree = _t.Add(Set(new Subtree(), "_tree", sub.Asset), _t.Root);
            var tree = _t.Build();

            tree.Tick(0.1f);
            tree.CollectRunning(_running);

            Assert.AreEqual(2, _running.Count);
            Assert.AreSame(Copy(tree, subtree), _running[0].Node);
            Assert.AreSame(Copy(tree, sub.Asset, leaf), _running[1].Node);
            Assert.AreEqual(1, _running[1].Depth);
        }

        [Test]
        public void TraceKeepsRecordingWhileAnyoneStillWatches()
        {
            _t.Add(new ScriptedLeaf(), _t.Root);
            var tree = _t.Build();

            BtTrace editor    = tree.StartTrace();
            BtTrace spectator = tree.StartTrace();
            Assert.AreSame(editor, spectator);

            tree.StopTrace();
            Assert.IsNotNull(tree.Trace);
            tree.StopTrace();
            Assert.IsNull(tree.Trace);

            tree.StopTrace();   // one too many is harmless
            Assert.IsNotNull(tree.StartTrace());
        }

        [Test]
        public void NodesAreNamedAfterTheirMenuPathOrTheirClass()
        {
            Assert.AreEqual("Sequence", BtNodeNames.Of(typeof(Sequence)));
            Assert.AreEqual("Scripted Leaf", BtNodeNames.Of(typeof(ScriptedLeaf)));
            Assert.AreEqual("Take Cover", BtNodeNames.Spaced("TakeCover"));
            Assert.AreEqual("Parallel · Require all", BtNodeNames.WithSummary(new Parallel()));
        }
    }
}
