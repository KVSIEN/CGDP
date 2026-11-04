using UnityEngine;
using CGD.Input;

namespace CGD.UI
{
    public abstract class HUDElement : MonoBehaviour
    {
        [Tooltip("The HUD palette. Leave empty to use the built-in defaults")]
        [SerializeField] private HUDTheme _theme;

        protected HUDTheme Theme => _theme != null ? _theme : HUDTheme.Fallback;

        [Tooltip("The key bindings, for elements that show which key does what. Leave empty and key hints show as unbound")]
        [SerializeField] private InputBindingSettings _bindings;

        // Subscribe in OnEnable and redraw hints on Changed: bindings can be rebound while playing.
        protected InputBindingSettings Bindings => _bindings;

        protected string KeyOf(GameAction action) =>
            _bindings != null ? _bindings.PromptFor(action) : InputPromptLabels.Unbound;

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
