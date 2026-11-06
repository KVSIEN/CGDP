using System;
using CGD.BehaviorTrees;

namespace CGD.Editor
{
    // Edits made outside the canvas (the key picker's menu) announce themselves here so the
    // window can refresh node summaries and re-validate.
    internal static class BtEditorEvents
    {
        public static event Action<BehaviorTreeAsset> AssetEdited;

        public static void RaiseAssetEdited(BehaviorTreeAsset asset) => AssetEdited?.Invoke(asset);
    }
}
