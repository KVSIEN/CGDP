using UnityEngine;

namespace CGD.UI
{
    public static class CanvasGroupExtensions
    {
        // Shows or hides a panel without deactivating it, so it keeps updating while hidden.
        // A read-only panel (a tooltip) passes interactive: false so it never takes clicks.
        public static void SetVisible(this CanvasGroup group, bool visible, bool interactive = true)
        {
            group.alpha          = visible ? 1f : 0f;
            group.blocksRaycasts = visible && interactive;
            group.interactable   = visible && interactive;
        }
    }
}
