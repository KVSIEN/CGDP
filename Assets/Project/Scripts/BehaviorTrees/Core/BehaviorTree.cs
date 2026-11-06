using System.Collections.Generic;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // One agent's running copy of a BehaviorTreeAsset (and of any subtrees it uses): its own
    // nodes, blackboard and clock. The owner ticks it (usually once per frame) and aborts it
    // when the agent dies.
    public sealed class BehaviorTree
    {
        // Each asset's copies by node id: the main tree and every subtree it pulled in.
        private readonly Dictionary<BehaviorTreeAsset, Dictionary<string, BtNode>> _copies = new();
        // Assets being instantiated right now, so a subtree that contains itself is refused.
        private readonly HashSet<BehaviorTreeAsset> _building = new();
        private BtRoot _root;

        private BehaviorTree(BehaviorTreeAsset asset, BtContext context)
        {
            Asset   = asset;
            Context = context;
            context.Tree = this;
        }

        public BehaviorTreeAsset Asset      { get; }
        public BtContext         Context    { get; }
        public Blackboard        Blackboard => Context.Blackboard;

        public void Tick(float deltaTime)
        {
            Context.DeltaTime  = deltaTime;
            Context.Time      += deltaTime;
            Context.TickCount++;
            using (Asset.Marker.Auto()) _root.Tick(Context);
        }

        // Stops whatever is running; the next Tick starts from the top.
        public void Abort() => _root.Abort(Context);

        // Abort and forget everything learned, e.g. when a pooled agent is reused.
        public void Reset()
        {
            Abort();
            Blackboard.Clear();
            foreach (var nodes in _copies.Values)
                foreach (BtNode node in nodes.Values) node.ResetState();
        }

        // The decision log, while one is being recorded (the editor starts it when it watches
        // this agent).
        public BtTrace Trace => Context.Trace;

        public BtTrace StartTrace(int capacity = 256) => Context.Trace ??= new BtTrace(Context, capacity);

        public void StopTrace() => Context.Trace = null;

        // Whether this tree runs the asset, as itself or as a subtree.
        public bool Uses(BehaviorTreeAsset asset) => _copies.ContainsKey(asset);

        // Debugging: this agent's copy of an asset node (the main tree's by default). A subtree
        // used in several places reports the first.
        public bool TryGetNode(string guid, out BtNode node) => TryGetNode(Asset, guid, out node);

        public bool TryGetNode(BehaviorTreeAsset asset, string guid, out BtNode node)
        {
            node = null;
            return _copies.TryGetValue(asset, out var nodes) && nodes.TryGetValue(guid, out node);
        }

        internal static BehaviorTree Create(BehaviorTreeAsset asset, GameObject owner, object agent)
        {
            var tree = new BehaviorTree(asset, new BtContext(owner, agent));
            tree._root = tree.Instantiate(asset);
            return tree._root != null ? tree : null;
        }

        // Copies an asset's nodes for this agent, links them and initialises them (a Subtree
        // node instantiates its own asset here). Null when the asset has no root, or would
        // contain itself.
        internal BtRoot Instantiate(BehaviorTreeAsset asset)
        {
            if (asset == null || !_building.Add(asset)) return null;

            var copies = new Dictionary<string, BtNode>();
            foreach (BtNode node in asset.Nodes)
                if (node != null) copies[node.Guid] = node.CreateRuntimeCopy();

            foreach (BtNode copy in copies.Values)
                if (copy is BtParentNode parent) parent.LinkChildren(copies);

            _copies.TryAdd(asset, copies);
            foreach (BtNode copy in copies.Values)
                copy.Initialize(Context);

            _building.Remove(asset);
            return asset.Root != null && copies.TryGetValue(asset.Root.Guid, out BtNode root) ? (BtRoot)root : null;
        }
    }
}
