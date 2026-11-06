using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Tests
{
    // Builds behavior tree assets in code for tests: add nodes under parents, set their
    // private serialized settings the way the inspector would, run the tree, and look up
    // each node's runtime copy.
    internal sealed class BtTestTree : IDisposable
    {
        public BtTestTree()
        {
            Asset = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            Asset.EnsureRoot();
        }

        public BehaviorTreeAsset Asset { get; }
        public BtRoot Root => Asset.Root;

        public T Add<T>(T node, BtNode parent) where T : BtNode
        {
            Asset.AddNode(node);
            ((BtParentNode)parent).AddChild(node.Guid);
            return node;
        }

        public BehaviorTree Build() => Asset.CreateInstance(null, null);

        public void Dispose() => UnityEngine.Object.DestroyImmediate(Asset);

        public static T Copy<T>(BehaviorTree tree, T assetNode) where T : BtNode => Copy(tree, tree.Asset, assetNode);

        public static T Copy<T>(BehaviorTree tree, BehaviorTreeAsset asset, T assetNode) where T : BtNode
        {
            Assert.IsTrue(tree.TryGetNode(asset, assetNode.Guid, out BtNode copy), $"No runtime copy of {assetNode.GetType().Name}");
            return (T)copy;
        }

        public static T Set<T>(T node, string field, object value) where T : BtNode
        {
            FieldInfo info = FindField(typeof(T), field);
            Assert.IsNotNull(info, $"{typeof(T).Name} has no field {field}");
            info.SetValue(node, value);
            return node;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            {
                FieldInfo info = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (info != null) return info;
            }
            return null;
        }
    }

    // A leaf whose result the test controls, counting its lifecycle calls.
    [Serializable]
    internal sealed class ScriptedLeaf : BtAction
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

    // A condition whose answer the test controls.
    [Serializable]
    internal sealed class ScriptedCondition : BtCondition
    {
        public bool Holds = true;
        [NonSerialized] public int Checks;

        protected override bool Check(BtContext context)
        {
            Checks++;
            return Holds;
        }
    }
}
