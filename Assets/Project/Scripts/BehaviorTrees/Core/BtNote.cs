using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // A sticky note on the editor canvas, for explaining a tree to the next person. Layout only.
    [Serializable]
    public sealed class BtNote
    {
        public string Title = "Note";
        public string Text;
        public Rect   Position = new(0f, 0f, 200f, 120f);
    }
}
