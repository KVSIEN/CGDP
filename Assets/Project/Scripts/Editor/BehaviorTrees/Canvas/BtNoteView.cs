using CGD.BehaviorTrees;
using UnityEditor.Experimental.GraphView;

namespace CGD.Editor
{
    // A sticky note on the canvas, backed by a BtNote in the asset.
    internal sealed class BtNoteView : StickyNote
    {
        public BtNoteView(BtNote data) : base(data.Position.position)
        {
            Data     = data;
            title    = data.Title;
            contents = data.Text;
            SetPosition(data.Position);
        }

        public BtNote Data { get; }
    }
}
