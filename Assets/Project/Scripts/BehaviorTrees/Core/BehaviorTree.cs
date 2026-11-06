using System.Collections.Generic;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // One agent's running copy of a BehaviorTreeAsset: its own nodes, blackboard and clock.
    // The owner ticks it (usually once per frame) and aborts it when the agent dies.
    public sealed class BehaviorTree
    {
        private readonly BtRoot _root;
        private readonly Dictionary<string, BtNode> _nodesByGuid;

        internal BehaviorTree(BehaviorTreeAsset asset, BtRoot root, Dictionary<string, BtNode> nodesByGuid, BtContext context)
        {
            Asset        = asset;
            _root        = root;
            _nodesByGuid = nodesByGuid;
            Context      = context;
        }

        public BehaviorTreeAsset Asset      { get; }
        public BtContext         Context    { get; }
        public Blackboard        Blackboard => Context.Blackboard;

        public void Tick(float deltaTime)
        {
            Context.DeltaTime  = deltaTime;
            Context.Time      += deltaTime;
            Context.TickCount++;
            _root.Tick(Context);
        }

        // Stops whatever is running; the next Tick starts from the top.
        public void Abort() => _root.Abort(Context);

        // Abort and forget everything learned, e.g. when a pooled agent is reused.
        public void Reset()
        {
            Abort();
            Blackboard.Clear();
        }

        // Debugging: this agent's copy of an asset node.
        public bool TryGetNode(string guid, out BtNode node) => _nodesByGuid.TryGetValue(guid, out node);

        internal static BehaviorTree Create(BehaviorTreeAsset asset, GameObject owner, object agent)
        {
            var context = new BtContext(owner, agent);
            var copies  = new Dictionary<string, BtNode>();

            foreach (BtNode node in asset.Nodes)
                if (node != null) copies[node.Guid] = node.CreateRuntimeCopy();

            foreach (BtNode copy in copies.Values)
                if (copy is BtParentNode parent) parent.LinkChildren(copies);

            foreach (BtNode copy in copies.Values)
                copy.Initialize(context);

            var root = asset.Root != null && copies.TryGetValue(asset.Root.Guid, out BtNode r) ? (BtRoot)r : null;
            return root != null ? new BehaviorTree(asset, root, copies, context) : null;
        }
    }
}
