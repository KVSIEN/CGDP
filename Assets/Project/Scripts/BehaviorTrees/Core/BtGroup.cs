using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // A titled frame around some nodes on the editor canvas ("Fight", "Routine"). Layout only.
    [Serializable]
    public sealed class BtGroup
    {
        public string       Title = "Group";
        public List<string> Nodes = new();
        // Where it sits when it holds no nodes.
        public Rect         Position = new(0f, 0f, 200f, 100f);
    }
}
