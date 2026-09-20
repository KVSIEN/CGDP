using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CGD.Core;
using CGD.Input;
using CGD.Player;

namespace CGD.UI
{
    /// <summary>
    /// ESC toggles this menu open/closed.
    /// Builds its own UGUI panel at runtime (same convention as the rest of the HUD via
    /// UIFactory) — requires a RectTransform under a Canvas, see SETUP.md.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private PlayerCamera        _camera;
        [SerializeField] private PlayerInputHandler  _input;
        [SerializeField] private InputBindingSettings _bindings;
        [SerializeField] private HUDManager          _hud;

        private const float W = 680f;
        private const float H = 520f;

        private bool _isOpen;
        private GameObject _panel;
        private SensitivitySection _sensitivity;
        private KeybindingSection  _keybindings;

        private void Awake()
        {
            BuildUI();
            _panel.SetActive(false);
        }

        private void Start() => _sensitivity.Load();

        private void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_keybindings.IsListening) { _keybindings.CancelRebind(); return; }
                if (_isOpen) Close(); else Open();
                return;
            }

            _keybindings.Tick(Time.unscaledDeltaTime);
        }

        private void Open()
        {
            _isOpen = true;
            _panel.SetActive(true);
            _input.InputEnabled = false;
            _hud.HideAll();
            CursorLock.Set(false);

            _sensitivity.Load();
        }

        private void Close()
        {
            _keybindings.CancelRebind();
            _isOpen = false;
            _panel.SetActive(false);
            _input.InputEnabled = true;
            _hud.ShowAll();
            CursorLock.Set(true);
        }

        private void BuildUI()
        {
            var self = GetComponent<RectTransform>();

            var panelGO = new GameObject("Panel", typeof(RectTransform));
            panelGO.transform.SetParent(self, false);
            _panel = panelGO;
            var panelRt = panelGO.GetComponent<RectTransform>();
            UIFactory.Stretch(panelRt);

            var windowGO = new GameObject("Window", typeof(RectTransform), typeof(Image));
            windowGO.transform.SetParent(panelRt, false);
            windowGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
            var windowRt = windowGO.GetComponent<RectTransform>();
            windowRt.anchorMin = windowRt.anchorMax = windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.anchoredPosition = Vector2.zero;
            windowRt.sizeDelta = new Vector2(W, H);

            _sensitivity = new SensitivitySection(windowRt, _camera);
            // The rebind overlay is parented to the panel (after the window) so it covers the whole window.
            _keybindings = new KeybindingSection(windowRt, panelRt, _input, _bindings, W);
            BuildBottomButtons(windowRt);
        }

        private void BuildBottomButtons(RectTransform window)
        {
            var resetBtn = UIFactory.MakeButton("ResetButton", window, "Reset Defaults", out _);
            UIFactory.Place(resetBtn.GetComponent<RectTransform>(), new Vector2(10f, -(H - 30f)), new Vector2(130f, 24f));
            resetBtn.onClick.AddListener(_keybindings.ResetToDefaults);

            var closeBtn = UIFactory.MakeButton("CloseButton", window, "Close", out _);
            UIFactory.Place(closeBtn.GetComponent<RectTransform>(), new Vector2(W - 90f, -(H - 30f)), new Vector2(78f, 24f));
            closeBtn.onClick.AddListener(Close);
        }
    }
}
