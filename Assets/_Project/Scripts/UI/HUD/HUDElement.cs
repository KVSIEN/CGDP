using UnityEngine;

namespace CGD.UI
{
    public abstract class HUDElement : MonoBehaviour
    {
        public bool IsVisible { get; protected set; } = true;

        // False keeps the element hidden when HUDManager.ShowAll restores the HUD
        // (e.g. panels the player opens on demand).
        public virtual bool ShowWithHud => true;

        public virtual void Show()
        {
            IsVisible = true;
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            IsVisible = false;
            gameObject.SetActive(false);
        }

        public virtual void Toggle()
        {
            if (IsVisible) Hide(); else Show();
        }

        public abstract void Refresh();
    }
}
