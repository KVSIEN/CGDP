using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CGD.Core;
using CGD.Flow;
using CGD.Input;
using CGD.Player;
using CGD.Settings;

namespace CGD.UI
{
    /// <summary>
    /// ESC toggles this menu open/closed; it doubles as the pause menu, so opening it pauses
    /// the game through GameFlow (when the scene has one) and it can't open while GameFlow
    /// is in a non-gameplay state such as GameOver.
    /// Builds its own UGUI panel at runtime (same convention as the rest of the HUD via
    /// UIFactory) — requires a RectTransform under a Canvas, see SETUP.md.
    /// Tabs: Controls (sensitivity, keybindings), Audio, Video and Accessibility. Audio, video
    /// and accessibility changes preview live and are saved when the menu closes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private PlayerCamera        _camera;
        [SerializeField] private PlayerInputHandler  _input;
        [SerializeField] private InputBindingSettings _bindings;
        [SerializeField] private HUDManager          _hud;

        private const float W = 680f;
        private const float H = 580f;
        private const float TabBarHeight = 40f;

        private static readonly string[] TabNames = { "Controls", "Audio", "Video", "Accessibility" };
        private static readonly Color TabSelectedColor   = new Color(0.3f, 0.55f, 1f, 0.95f);
        private static readonly Color TabUnselectedColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);

        private enum Tab { Controls, Audio, Video, Accessibility }

        private bool _isOpen;
        private GameObject _panel;
        private SensitivitySection   _sensitivity;
        private KeybindingSection    _keybindings;
        private AudioSection         _audio;
        private VideoSection         _video;
        private AccessibilitySection _accessibility;

        private GameObject[] _pages;
        private Image[]      _tabImages;
        private Tab          _activeTab;

        // Edited copy of GameSettings.Current; applied on every change, saved on close.
        private SettingsData _draft;

        private void Awake()
        {
            BuildUI();
            _panel.SetActive(false);
        }

        private void Start() => _sensitivity.Load();

        private void Update()
        {
            // While rebinding, KeybindingSection owns Escape (tap = cancel, hold = clear).
            if (_keybindings.IsListening)
            {
                _keybindings.Tick(Time.unscaledDeltaTime);
                return;
            }

            // Escape belongs to the dev console while it is being typed into.
            if (Keyboard.current.escapeKey.wasPressedThisFrame && _input.TextEntryActive) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_isOpen) Close(); else if (CanOpen) Open();
                return;
            }

            _keybindings.Tick(Time.unscaledDeltaTime);
        }

        private static bool CanOpen =>
            GameFlow.Instance == null || GameFlow.Instance.State == GameState.Playing;

        private void Open()
        {
            GameFlow.Instance?.Pause();
            _isOpen = true;
            _panel.SetActive(true);
            _input.InputEnabled = false;
            _hud.HideAll();
            CursorLock.Set(false);

            _sensitivity.Load();
            _draft = GameSettings.Current.Clone();
            _audio.Load(_draft);
            _video.Load(_draft);
            _accessibility.Load(_draft);
            ShowTab(_activeTab);
        }

        private void Close()
        {
            _keybindings.CancelRebind();
            GameSettings.Save();
            _isOpen = false;
            _panel.SetActive(false);
            _input.InputEnabled = true;
            _hud.ShowAll();
            CursorLock.Set(true);
            GameFlow.Instance?.Resume();
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

            BuildTabs(windowRt);

            var controls  = _pages[(int)Tab.Controls].GetComponent<RectTransform>();
            _sensitivity  = new SensitivitySection(controls, _camera);
            // The rebind overlay is parented to the panel (after the window) so it covers the whole window.
            _keybindings  = new KeybindingSection(controls, panelRt, _input, _bindings, W);
            _audio         = new AudioSection(Page(Tab.Audio), ApplyDraft);
            _video         = new VideoSection(Page(Tab.Video), ApplyDraft);
            _accessibility = new AccessibilitySection(Page(Tab.Accessibility), ApplyDraft);
            BuildBottomButtons(windowRt);
            ShowTab(Tab.Controls);
        }

        private RectTransform Page(Tab tab) => _pages[(int)tab].GetComponent<RectTransform>();

        private void ApplyDraft() => GameSettings.Apply(_draft);

        private void BuildTabs(RectTransform window)
        {
            const float TabWidth = 120f;
            _pages     = new GameObject[TabNames.Length];
            _tabImages = new Image[TabNames.Length];

            for (int i = 0; i < TabNames.Length; i++)
            {
                var tab = (Tab)i;
                var button = UIFactory.MakeButton(TabNames[i] + "Tab", window, TabNames[i], out _);
                UIFactory.Place(button.GetComponent<RectTransform>(), new Vector2(10f + i * (TabWidth + 6f), -10f), new Vector2(TabWidth, 24f));
                button.onClick.AddListener(() => ShowTab(tab));
                _tabImages[i] = button.GetComponent<Image>();

                // Pages keep the coordinates the sections were laid out in, just below the tab bar.
                var page = new GameObject(TabNames[i] + "Page", typeof(RectTransform));
                page.transform.SetParent(window, false);
                UIFactory.Place(page.GetComponent<RectTransform>(), new Vector2(0f, -TabBarHeight), new Vector2(W, H - TabBarHeight - 40f));
                _pages[i] = page;
            }
        }

        private void ShowTab(Tab tab)
        {
            // Leaving Controls mid-rebind would leave the listener capturing keys on a hidden page.
            if (tab != Tab.Controls) _keybindings.CancelRebind();

            _activeTab = tab;
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].SetActive(i == (int)tab);
                _tabImages[i].color = i == (int)tab ? TabSelectedColor : TabUnselectedColor;
            }
        }

        private void ResetActiveTab()
        {
            switch (_activeTab)
            {
                case Tab.Controls:      _keybindings.ResetToDefaults();   break;
                case Tab.Audio:         _audio.ResetToDefaults();         break;
                case Tab.Video:         _video.ResetToDefaults();         break;
                case Tab.Accessibility: _accessibility.ResetToDefaults(); break;
            }
        }

        private void BuildBottomButtons(RectTransform window)
        {
            var resetBtn = UIFactory.MakeButton("ResetButton", window, "Reset Defaults", out _);
            UIFactory.Place(resetBtn.GetComponent<RectTransform>(), new Vector2(10f, -(H - 30f)), new Vector2(130f, 24f));
            resetBtn.onClick.AddListener(ResetActiveTab);

            var closeBtn = UIFactory.MakeButton("CloseButton", window, "Close", out _);
            UIFactory.Place(closeBtn.GetComponent<RectTransform>(), new Vector2(W - 90f, -(H - 30f)), new Vector2(78f, 24f));
            closeBtn.onClick.AddListener(Close);
        }
    }
}
