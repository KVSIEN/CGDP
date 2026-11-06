using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Everything a node may use while it runs: the agent it drives, the agent's blackboard,
    // and the tree's clock. Nodes read the agent once in OnInitialize and keep it.
    public sealed class BtContext
    {
        internal BtContext(GameObject owner, object agent)
        {
            Owner = owner;
            Agent = agent;
        }

        public GameObject Owner      { get; }
        // The component the tree drives (e.g. EnemyAI). Nodes cast it to what they need.
        public object     Agent      { get; }
        public Blackboard Blackboard { get; } = new();

        // Seconds since the last tick, and seconds the tree has run (paused while the agent
        // doesn't tick it, e.g. while stunned).
        public float DeltaTime { get; internal set; }
        public float Time      { get; internal set; }

        // Ticks so far. Nodes stamp it when they run so the editor can tell live nodes apart.
        public int TickCount { get; internal set; }
    }
}
