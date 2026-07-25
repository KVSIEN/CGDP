using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ESC toggles this menu open/closed.
/// Builds its own UGUI panel at runtime (same convention as the rest of the HUD via
/// HudUIFactory) — requires a RectTransform under a Canvas, see SETUP.md.
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
    private const float RowHeight = 28f;

    private static readonly string[] ModeLabels = { "Press", "Hold", "Toggle", "Dbl" };
    private static readonly Color ModeSelectedColor   = new Color(0.3f, 0.55f, 1f, 0.95f);
    private static readonly Color ModeUnselectedColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);

    private bool _isOpen;

    private GameObject _panel;
    private GameObject _rebindOverlay;
    private TextMeshProUGUI _conflictText;

    private Slider _mouseSlider;
    private TMP_InputField _mouseField;
    private Slider _gamepadSlider;
    private TMP_InputField _gamepadField;

    private TextMeshProUGUI[] _primaryLabels;
    private TextMeshProUGUI[] _secondaryLabels;
    private Image[][] _modeButtonImages;

    private float _pendingMouse;
    private float _pendingGamepad;

    // Rebind state: -1 = not listening
    private int  _listeningAction = -1;
    private bool _listeningPrimary;
    private int  _listenStartFrame;
    private bool _rebindStarted;
    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

    // Transient warning shown when a rebind attempt conflicts with an existing binding
    private string _conflictMessage;
    private float  _conflictMessageTimer;

    private void Awake()
    {
        BuildUI();
        _panel.SetActive(false);
    }

    private void Start()
    {
        SettingsSave.LoadSensitivity(out _pendingMouse, out _pendingGamepad);
        SyncSensitivityWidgets();
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_listeningAction >= 0) { CancelRebind(); return; }
            if (_isOpen) Close(); else Open();
            return;
        }

        // Defer starting the interactive rebind by one frame so the mouse click that
        // opened the listening UI isn't immediately captured as the new binding.
        if (_listeningAction >= 0 && !_rebindStarted && Time.frameCount > _listenStartFrame)
        {
            _rebindStarted = true;
            BeginRebind((GameAction)_listeningAction, _listeningPrimary);
        }

        _rebindOverlay.SetActive(_listeningAction >= 0);

        if (_conflictMessageTimer > 0f)
        {
            _conflictMessageTimer -= Time.unscaledDeltaTime;
            _conflictText.gameObject.SetActive(true);
            _conflictText.text = _conflictMessage;
        }
        else
        {
            _conflictText.gameObject.SetActive(false);
        }
    }

    // ── Rebind state machine (unchanged by the OnGUI -> UGUI rebuild) ──────

    private void BeginRebind(GameAction action, bool primary)
    {
        var inputAction = _input.GetInputAction(action);
        int bindingIndex = primary ? 0 : 1;

        _rebindOperation = inputAction.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Keyboard>/escape")
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnMatchWaitForAnother(0.05f)
            .OnComplete(op => OnRebindCompleted(action, primary, bindingIndex, op))
            .OnCancel(op => op.Dispose())
            .Start();
    }

    private void OnRebindCompleted(GameAction action, bool primary, int bindingIndex,
        InputActionRebindingExtensions.RebindingOperation op)
    {
        string path = op.action.bindings[bindingIndex].effectivePath;
        op.Dispose();
        _rebindOperation = null;
        _rebindStarted   = false;

        GameAction? conflict = FindConflict(action, primary, path);
        if (conflict.HasValue)
        {
            _input.RebuildActions(); // discard the transient override — nothing was persisted
            _conflictMessage      = $"{BindingLabel(path)} is already bound to {conflict.Value}";
            _conflictMessageTimer = 2.5f;
            _listeningAction      = -1;

            int row = RowIndexOf(action);
            if (row >= 0) RefreshRow(row);
            return;
        }

        WriteRebind(action, primary, path);
    }

    private void CancelRebind()
    {
        int prevAction = _listeningAction;

        _rebindOperation?.Cancel();
        _rebindOperation      = null;
        _listeningAction      = -1;
        _rebindStarted        = false;
        _conflictMessageTimer = 0f;

        if (prevAction >= 0)
        {
            int row = RowIndexOf((GameAction)prevAction);
            if (row >= 0) RefreshRow(row);
        }
    }

    // Returns the action already using this control path, if any — excluding the exact
    // slot currently being rebound (so re-picking the same input isn't a conflict with
    // itself, but colliding with the binding's *other* slot still is).
    private GameAction? FindConflict(GameAction action, bool primary, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        foreach (var b in _bindings.Bindings)
        {
            bool isOwnPrimarySlot   = b.Action == action && primary;
            bool isOwnSecondarySlot = b.Action == action && !primary;

            if (!isOwnPrimarySlot   && b.PrimaryPath   == path) return b.Action;
            if (!isOwnSecondarySlot && b.SecondaryPath == path) return b.Action;
        }
        return null;
    }

    private void WriteRebind(GameAction action, bool primary, string path)
    {
        for (int i = 0; i < _bindings.Bindings.Count; i++)
        {
            if (_bindings.Bindings[i].Action != action) continue;
            var b = _bindings.Bindings[i];
            if (primary) b.PrimaryPath   = path;
            else         b.SecondaryPath = path;
            _bindings.Bindings[i] = b;
            break;
        }

        _input.RebuildActions();
        SettingsSave.SaveBindings(_bindings);
        _listeningAction      = -1;
        _conflictMessageTimer = 0f;

        int row = RowIndexOf(action);
        if (row >= 0) RefreshRow(row);
    }

    private void StartListening(GameAction action, bool primary)
    {
        _listeningAction      = (int)action;
        _listeningPrimary     = primary;
        _listenStartFrame     = Time.frameCount;
        _rebindStarted        = false;
        _conflictMessageTimer = 0f;

        int row = RowIndexOf(action);
        if (row >= 0) RefreshRow(row);
    }

    private int RowIndexOf(GameAction action)
    {
        for (int i = 0; i < _bindings.Bindings.Count; i++)
            if (_bindings.Bindings[i].Action == action) return i;
        return -1;
    }

    private static string BindingLabel(string path) =>
        string.IsNullOrEmpty(path) ? "—" : InputControlPath.ToHumanReadableString(path);

    // ── Open / Close ────────────────────────────────────────────────────────

    private void Open()
    {
        _isOpen = true;
        _panel.SetActive(true);
        _input.InputEnabled = false;
        _hud.HideAll();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        SettingsSave.LoadSensitivity(out _pendingMouse, out _pendingGamepad);
        SyncSensitivityWidgets();
    }

    private void Close()
    {
        CancelRebind();
        _isOpen = false;
        _panel.SetActive(false);
        _input.InputEnabled = true;
        _hud.ShowAll();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void SyncSensitivityWidgets()
    {
        _mouseSlider.SetValueWithoutNotify(_pendingMouse);
        _mouseField.SetTextWithoutNotify(_pendingMouse.ToString("F2"));
        _gamepadSlider.SetValueWithoutNotify(_pendingGamepad);
        _gamepadField.SetTextWithoutNotify(_pendingGamepad.ToString("F0"));
    }

    private void OnResetClicked()
    {
        CancelRebind();
        _bindings.ResetToDefaults();
        _input.RebuildActions();
        SettingsSave.DeleteBindings();
        RefreshAllRows();
    }

    // ── UI construction ─────────────────────────────────────────────────────

    private void BuildUI()
    {
        var self = GetComponent<RectTransform>();

        var panelGO = new GameObject("Panel", typeof(RectTransform));
        panelGO.transform.SetParent(self, false);
        _panel = panelGO;
        HudUIFactory.Stretch(panelGO.GetComponent<RectTransform>());

        var windowGO = new GameObject("Window", typeof(RectTransform), typeof(Image));
        windowGO.transform.SetParent(_panel.transform, false);
        windowGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        var windowRt = windowGO.GetComponent<RectTransform>();
        windowRt.anchorMin = windowRt.anchorMax = windowRt.pivot = new Vector2(0.5f, 0.5f);
        windowRt.anchoredPosition = Vector2.zero;
        windowRt.sizeDelta = new Vector2(W, H);

        BuildSensitivitySection(windowRt);
        BuildKeybindingSection(windowRt);
        BuildBottomButtons(windowRt);
        BuildRebindOverlay(_panel.GetComponent<RectTransform>());
    }

    private void BuildSensitivitySection(RectTransform window)
    {
        var title = HudUIFactory.MakeText("SensitivityHeader", window);
        title.text = "── Sensitivity ──";
        title.fontSize = 13f;
        HudUIFactory.Place(title.rectTransform, new Vector2(10f, -20f), new Vector2(580f, 20f));

        var mouseLabel = HudUIFactory.MakeText("MouseLabel", window);
        mouseLabel.text = "Mouse:";
        mouseLabel.fontSize = 12f;
        HudUIFactory.Place(mouseLabel.rectTransform, new Vector2(10f, -44f), new Vector2(60f, 20f));

        _mouseSlider = HudUIFactory.MakeSlider("MouseSlider", window);
        HudUIFactory.Place(_mouseSlider.GetComponent<RectTransform>(), new Vector2(75f, -48f), new Vector2(330f, 16f));
        _mouseSlider.minValue = 0.1f;
        _mouseSlider.maxValue = 10f;

        _mouseField = HudUIFactory.MakeInputField("MouseField", window);
        HudUIFactory.Place(_mouseField.GetComponent<RectTransform>(), new Vector2(415f, -44f), new Vector2(70f, 20f));
        _mouseField.contentType = TMP_InputField.ContentType.DecimalNumber;

        _mouseSlider.onValueChanged.AddListener(v =>
        {
            _pendingMouse = v;
            _mouseField.SetTextWithoutNotify(v.ToString("F2"));
        });
        _mouseField.onValueChanged.AddListener(str =>
        {
            if (float.TryParse(str, out float v))
            {
                _pendingMouse = Mathf.Clamp(v, 0.1f, 10f);
                _mouseSlider.SetValueWithoutNotify(_pendingMouse);
            }
        });

        var gamepadLabel = HudUIFactory.MakeText("GamepadLabel", window);
        gamepadLabel.text = "Gamepad:";
        gamepadLabel.fontSize = 12f;
        HudUIFactory.Place(gamepadLabel.rectTransform, new Vector2(10f, -70f), new Vector2(60f, 20f));

        _gamepadSlider = HudUIFactory.MakeSlider("GamepadSlider", window);
        HudUIFactory.Place(_gamepadSlider.GetComponent<RectTransform>(), new Vector2(75f, -74f), new Vector2(330f, 16f));
        _gamepadSlider.minValue = 10f;
        _gamepadSlider.maxValue = 300f;

        _gamepadField = HudUIFactory.MakeInputField("GamepadField", window);
        HudUIFactory.Place(_gamepadField.GetComponent<RectTransform>(), new Vector2(415f, -70f), new Vector2(70f, 20f));
        _gamepadField.contentType = TMP_InputField.ContentType.DecimalNumber;

        _gamepadSlider.onValueChanged.AddListener(v =>
        {
            _pendingGamepad = v;
            _gamepadField.SetTextWithoutNotify(v.ToString("F0"));
        });
        _gamepadField.onValueChanged.AddListener(str =>
        {
            if (float.TryParse(str, out float v))
            {
                _pendingGamepad = Mathf.Clamp(v, 10f, 300f);
                _gamepadSlider.SetValueWithoutNotify(_pendingGamepad);
            }
        });

        var applyBtn = HudUIFactory.MakeButton("ApplyButton", window, "Apply Sensitivity", out _);
        HudUIFactory.Place(applyBtn.GetComponent<RectTransform>(), new Vector2(170f, -98f), new Vector2(140f, 22f));
        applyBtn.onClick.AddListener(() =>
        {
            _camera.SetSensitivity(_pendingMouse, _pendingGamepad);
            SettingsSave.SaveSensitivity(_pendingMouse, _pendingGamepad);
        });
    }

    private void BuildKeybindingSection(RectTransform window)
    {
        var header = HudUIFactory.MakeText("KeybindingsHeader", window);
        header.text = "── Keybindings ──";
        header.fontSize = 13f;
        HudUIFactory.Place(header.rectTransform, new Vector2(10f, -132f), new Vector2(580f, 20f));

        MakeColumnHeader(window, "Action",    10f,  135f);
        MakeColumnHeader(window, "Primary",   150f, 120f);
        MakeColumnHeader(window, "Secondary", 275f, 120f);
        MakeColumnHeader(window, "Mode",      400f, 200f);

        int count = _bindings.Bindings.Count;
        _primaryLabels     = new TextMeshProUGUI[count];
        _secondaryLabels   = new TextMeshProUGUI[count];
        _modeButtonImages  = new Image[count][];

        float contentHeight = count * RowHeight;
        var content = BuildScrollView(window, new Vector2(10f, -174f), new Vector2(W - 20f, 310f), contentHeight, W - 40f);

        for (int i = 0; i < count; i++)
            BuildBindingRow(i, content, i * RowHeight);
    }

    private void MakeColumnHeader(RectTransform window, string label, float x, float width)
    {
        var text = HudUIFactory.MakeText("Header_" + label, window);
        text.text = label;
        text.fontSize = 12f;
        HudUIFactory.Place(text.rectTransform, new Vector2(x, -154f), new Vector2(width, 20f));
    }

    private RectTransform BuildScrollView(RectTransform parent, Vector2 pos, Vector2 size, float contentHeight, float contentWidth)
    {
        var scrollGO = new GameObject("BindingsScroll", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollGO.transform.SetParent(parent, false);
        var scrollRt = scrollGO.GetComponent<RectTransform>();
        HudUIFactory.Place(scrollRt, pos, size);

        scrollGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
        scrollGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(scrollRt, false);
        var contentRt = contentGO.GetComponent<RectTransform>();
        contentRt.anchorMin = contentRt.anchorMax = new Vector2(0f, 1f);
        contentRt.pivot = new Vector2(0f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(contentWidth, contentHeight);

        var scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.content = contentRt;
        scrollRect.viewport = scrollRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return contentRt;
    }

    private void BuildBindingRow(int rowIndex, RectTransform content, float rowY)
    {
        var b = _bindings.Bindings[rowIndex];

        var label = HudUIFactory.MakeText("Action_" + rowIndex, content);
        label.text = b.Action.ToString();
        label.fontSize = 12f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        HudUIFactory.Place(label.rectTransform, new Vector2(0f, -rowY), new Vector2(140f, 24f));

        var primaryBtn = HudUIFactory.MakeButton("Primary_" + rowIndex, content, "", out var primaryLabel);
        HudUIFactory.Place(primaryBtn.GetComponent<RectTransform>(), new Vector2(145f, -rowY), new Vector2(120f, 24f));
        primaryBtn.onClick.AddListener(() => StartListening(b.Action, true));
        _primaryLabels[rowIndex] = primaryLabel;

        var secondaryBtn = HudUIFactory.MakeButton("Secondary_" + rowIndex, content, "", out var secondaryLabel);
        HudUIFactory.Place(secondaryBtn.GetComponent<RectTransform>(), new Vector2(270f, -rowY), new Vector2(120f, 24f));
        secondaryBtn.onClick.AddListener(() => StartListening(b.Action, false));
        _secondaryLabels[rowIndex] = secondaryLabel;

        var modeImages = new Image[ModeLabels.Length];
        float modeBtnWidth = 195f / ModeLabels.Length;
        for (int m = 0; m < ModeLabels.Length; m++)
        {
            int modeIndex = m;
            var modeBtn = HudUIFactory.MakeButton("Mode_" + rowIndex + "_" + m, content, ModeLabels[m], out var modeLabel);
            modeLabel.fontSize = 10f;
            HudUIFactory.Place(modeBtn.GetComponent<RectTransform>(),
                new Vector2(395f + m * modeBtnWidth, -rowY), new Vector2(modeBtnWidth - 2f, 24f));
            modeBtn.onClick.AddListener(() => OnModeClicked(rowIndex, modeIndex));
            modeImages[m] = (Image)modeBtn.targetGraphic;
        }
        _modeButtonImages[rowIndex] = modeImages;

        RefreshRow(rowIndex);
    }

    private void BuildBottomButtons(RectTransform window)
    {
        var resetBtn = HudUIFactory.MakeButton("ResetButton", window, "Reset Defaults", out _);
        HudUIFactory.Place(resetBtn.GetComponent<RectTransform>(), new Vector2(10f, -(H - 30f)), new Vector2(130f, 24f));
        resetBtn.onClick.AddListener(OnResetClicked);

        var closeBtn = HudUIFactory.MakeButton("CloseButton", window, "Close", out _);
        HudUIFactory.Place(closeBtn.GetComponent<RectTransform>(), new Vector2(W - 90f, -(H - 30f)), new Vector2(78f, 24f));
        closeBtn.onClick.AddListener(Close);
    }

    private void BuildRebindOverlay(RectTransform parent)
    {
        var overlayGO = new GameObject("RebindOverlay", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parent, false);
        _rebindOverlay = overlayGO;

        var overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImg.raycastTarget = true; // block clicks to the window while listening
        HudUIFactory.Stretch(overlayGO.GetComponent<RectTransform>());

        var promptText = HudUIFactory.MakeText("Prompt", overlayGO.GetComponent<RectTransform>());
        promptText.text = "Press any key, mouse, or gamepad button\n(Escape to cancel)";
        promptText.fontSize = 16f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        promptText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        promptText.rectTransform.anchoredPosition = new Vector2(0f, 25f);
        promptText.rectTransform.sizeDelta = new Vector2(400f, 50f);

        _conflictText = HudUIFactory.MakeText("ConflictMessage", overlayGO.GetComponent<RectTransform>());
        _conflictText.fontSize = 14f;
        _conflictText.color = new Color(1f, 0.3f, 0.3f, 1f);
        _conflictText.alignment = TextAlignmentOptions.Center;
        _conflictText.rectTransform.anchorMin = _conflictText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _conflictText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _conflictText.rectTransform.anchoredPosition = new Vector2(0f, -25f);
        _conflictText.rectTransform.sizeDelta = new Vector2(400f, 30f);

        _rebindOverlay.SetActive(false);
    }

    // ── Per-row refresh ──────────────────────────────────────────────────────

    private void RefreshAllRows()
    {
        for (int i = 0; i < _bindings.Bindings.Count; i++)
            RefreshRow(i);
    }

    private void RefreshRow(int rowIndex)
    {
        var b = _bindings.Bindings[rowIndex];
        bool waitPri = _listeningAction == (int)b.Action && _listeningPrimary;
        bool waitSec = _listeningAction == (int)b.Action && !_listeningPrimary;

        _primaryLabels[rowIndex].text   = waitPri ? "▪▪▪" : BindingLabel(b.PrimaryPath);
        _secondaryLabels[rowIndex].text = waitSec ? "▪▪▪" : BindingLabel(b.SecondaryPath);

        var images = _modeButtonImages[rowIndex];
        for (int m = 0; m < images.Length; m++)
            images[m].color = (int)b.Mode == m ? ModeSelectedColor : ModeUnselectedColor;
    }

    private void OnModeClicked(int rowIndex, int modeIndex)
    {
        var b = _bindings.Bindings[rowIndex];
        b.Mode = (InputActionMode)modeIndex;
        _bindings.Bindings[rowIndex] = b;
        _input.SetMode(b.Action, b.Mode);
        SettingsSave.SaveBindings(_bindings);
        RefreshRow(rowIndex);
    }
}
