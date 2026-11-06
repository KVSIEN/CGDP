using CGD.BehaviorTrees;
using UnityEditor.Experimental.GraphView;

namespace CGD.Editor
{
    // A titled frame around nodes on the canvas, backed by a BtGroup in the asset.
    internal sealed class BtGroupView : Group
    {
        public BtGroupView(BtGroup data)
        {
            Data  = data;
            title = data.Title;
            SetPosition(data.Position);
        }

        public BtGroup Data { get; }
    }
}
