using System;
using CGD.BehaviorTrees;

namespace CGD.Editor
{
    // Edits made outside the canvas announce themselves here so the window can catch up:
    // AssetEdited for a changed setting (the key picker's menu: summaries are refreshed),
    // StructureEdited for added, removed or moved nodes (the Behavior Composer: the canvas is
    // rebuilt).
    internal static class BtEditorEvents
    {
        public static event Action<BehaviorTreeAsset> AssetEdited;
        public static event Action<BehaviorTreeAsset> StructureEdited;

        public static void RaiseAssetEdited(BehaviorTreeAsset asset) => AssetEdited?.Invoke(asset);
        public static void RaiseStructureEdited(BehaviorTreeAsset asset) => StructureEdited?.Invoke(asset);
    }
}
