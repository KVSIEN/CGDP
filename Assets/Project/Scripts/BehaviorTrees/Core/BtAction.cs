using System;

namespace CGD.BehaviorTrees
{
    // A leaf that does something in the world: move, attack, wait. May take several ticks.
    [Serializable]
    public abstract class BtAction : BtNode { }
}
