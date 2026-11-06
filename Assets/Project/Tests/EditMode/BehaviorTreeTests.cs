using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Tests
{
    public class BehaviorTreeTests
    {
        // A leaf whose result the test controls, counting its lifecycle calls.
        [Serializable]
        private sealed class ScriptedLeaf : BtAction
        {
            public BtStatus Result = BtStatus.Success;
            [NonSerialized] public int Ticks, Enters, Exits;

            protected override void OnEnter(BtContext context) => Enters++;
            protected override void OnExit(BtContext context) => Exits++;

            protected override BtStatus OnTick(BtContext context)
            {
                Ticks++;
                return Result;
            }
        }

        private BehaviorTreeAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            _asset.EnsureRoot();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_asset);

        [Test]
        public void SequenceSucceedsWhenEveryChildSucceeds()
        {
            var sequence = Add(new Sequence(), _asset.Root);
            var a = Add(new ScriptedLeaf(), sequence);
            var b = Add(new ScriptedLeaf(), sequence);
            var tree = Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Success, Copy(tree, sequence).LastStatus);
            Assert.AreEqual(1, Copy(tree, a).Ticks);
            Assert.AreEqual(1, Copy(tree, b).Ticks);
        }

        [Test]
        public void SequenceStopsAtTheFirstFailure()
        {
            var sequence = Add(new Sequence(), _asset.Root);
            Add(new ScriptedLeaf { Result = BtStatus.Failure }, sequence);
            var later = Add(new ScriptedLeaf(), sequence);
            var tree = Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Failure, Copy(tree, sequence).LastStatus);
            Assert.AreEqual(0, Copy(tree, later).Ticks);
        }

        [Test]
        public void SelectorRunsTheFirstChildThatDoesNotFail()
        {
            var selector = Add(new Selector(), _asset.Root);
            Add(new ScriptedLeaf { Result = BtStatus.Failure }, selector);
            var second = Add(new ScriptedLeaf { Result = BtStatus.Running }, selector);
            var third  = Add(new ScriptedLeaf(), selector);
            var tree = Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Running, Copy(tree, selector).LastStatus);
            Assert.AreEqual(1, Copy(tree, second).Ticks);
            Assert.AreEqual(0, Copy(tree, third).Ticks);
        }

        [Test]
        public void ReactiveSelectorLetsAHigherChildInterrupt()
        {
            var selector = Add(Configure(new Selector(), "_reactive", true), _asset.Root);
            var urgent   = Add(new ScriptedLeaf { Result = BtStatus.Failure }, selector);
            var routine  = Add(new ScriptedLeaf { Result = BtStatus.Running }, selector);
            var tree = Build();

            tree.Tick(0.1f);
            Assert.IsTrue(Copy(tree, routine).IsRunning);

            Copy(tree, urgent).Result = BtStatus.Running;
            tree.Tick(0.1f);

            Assert.IsFalse(Copy(tree, routine).IsRunning);
            Assert.AreEqual(1, Copy(tree, routine).Exits);
            Assert.IsTrue(Copy(tree, urgent).IsRunning);
        }

        [Test]
        public void ReactiveSequenceAbortsWhenAnEarlierConditionFails()
        {
            var sequence  = Add(Configure(new Sequence(), "_reactive", true), _asset.Root);
            var condition = Add(new ScriptedLeaf(), sequence);
            var action    = Add(new ScriptedLeaf { Result = BtStatus.Running }, sequence);
            var tree = Build();

            tree.Tick(0.1f);
            Assert.IsTrue(Copy(tree, action).IsRunning);

            Copy(tree, condition).Result = BtStatus.Failure;
            tree.Tick(0.1f);

            Assert.IsFalse(Copy(tree, action).IsRunning);
            Assert.AreEqual(BtStatus.Failure, Copy(tree, sequence).LastStatus);
        }

        [Test]
        public void NonReactiveSelectorKeepsRunningTheSameChild()
        {
            var selector = Add(new Selector(), _asset.Root);
            var urgent   = Add(new ScriptedLeaf { Result = BtStatus.Failure }, selector);
            var routine  = Add(new ScriptedLeaf { Result = BtStatus.Running }, selector);
            var tree = Build();

            tree.Tick(0.1f);
            Copy(tree, urgent).Result = BtStatus.Success;
            tree.Tick(0.1f);

            Assert.AreEqual(1, Copy(tree, urgent).Ticks);
            Assert.AreEqual(2, Copy(tree, routine).Ticks);
        }

        [Test]
        public void RepeatRunsItsChildTheSetNumberOfTimes()
        {
            var repeat = Add(Configure(new Repeat(), "_count", 3), _asset.Root);
            var leaf   = Add(new ScriptedLeaf(), repeat);
            var tree = Build();

            tree.Tick(0.1f);
            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Running, Copy(tree, repeat).LastStatus);

            tree.Tick(0.1f);
            Assert.AreEqual(BtStatus.Success, Copy(tree, repeat).LastStatus);
            Assert.AreEqual(3, Copy(tree, leaf).Ticks);
        }

        [Test]
        public void InverterSwapsSuccessAndFailure()
        {
            var inverter = Add(new Inverter(), _asset.Root);
            Add(new ScriptedLeaf { Result = BtStatus.Failure }, inverter);
            var tree = Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Success, Copy(tree, inverter).LastStatus);
        }

        [Test]
        public void CooldownFailsWithoutRunningItsChildUntilItIsOver()
        {
            var cooldown = Add(Configure(new Cooldown(), "_seconds", 1f), _asset.Root);
            var leaf     = Add(new ScriptedLeaf(), cooldown);
            var tree = Build();

            tree.Tick(0.1f);
            tree.Tick(0.5f);
            Assert.AreEqual(BtStatus.Failure, Copy(tree, cooldown).LastStatus);
            Assert.AreEqual(1, Copy(tree, leaf).Ticks);

            tree.Tick(0.6f);
            Assert.AreEqual(2, Copy(tree, leaf).Ticks);
        }

        [Test]
        public void ParallelRequireOneSucceedsAndAbortsTheRest()
        {
            var parallel = Add(Configure(new Parallel(), "_policy", ParallelPolicy.RequireOne), _asset.Root);
            var slow = Add(new ScriptedLeaf { Result = BtStatus.Running }, parallel);
            Add(new ScriptedLeaf(), parallel);
            var tree = Build();

            tree.Tick(0.1f);

            Assert.AreEqual(BtStatus.Success, Copy(tree, parallel).LastStatus);
            Assert.IsFalse(Copy(tree, slow).IsRunning);
        }

        [Test]
        public void AgentsRunIndependentCopies()
        {
            var leaf = Add(new ScriptedLeaf { Result = BtStatus.Running }, _asset.Root);
            var first  = Build();
            var second = Build();

            first.Tick(0.1f);

            Assert.AreEqual(1, Copy(first, leaf).Ticks);
            Assert.AreEqual(0, Copy(second, leaf).Ticks);
            Assert.AreEqual(0, ((ScriptedLeaf)_asset.FindNode(leaf.Guid)).Ticks);
        }

        [Test]
        public void BlackboardFlagsRoundTrip()
        {
            var sequence = Add(new Sequence(), _asset.Root);
            Add(Configure(new SetFlag(), "_key", "Seen"), sequence);
            Add(Configure(new CheckFlag(), "_key", "Seen"), sequence);
            var tree = Build();

            tree.Tick(0.1f);

            Assert.IsTrue(tree.Blackboard.GetBool("Seen"));
            Assert.AreEqual(BtStatus.Success, Copy(tree, sequence).LastStatus);
        }

        // --- Helpers ---------------------------------------------------------------

        private T Add<T>(T node, BtNode parent) where T : BtNode
        {
            _asset.AddNode(node);
            ((BtParentNode)parent).AddChild(node.Guid);
            return node;
        }

        private BehaviorTree Build() => _asset.CreateInstance(null, null);

        private static T Copy<T>(BehaviorTree tree, T assetNode) where T : BtNode
        {
            Assert.IsTrue(tree.TryGetNode(assetNode.Guid, out BtNode copy));
            return (T)copy;
        }

        // Authored settings are private serialized fields; tests set them the way the inspector would.
        private static T Configure<T>(T node, string field, object value) where T : BtNode
        {
            FieldInfo info = typeof(T).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"{typeof(T).Name} has no field {field}");
            info.SetValue(node, value);
            return node;
        }
    }
}
