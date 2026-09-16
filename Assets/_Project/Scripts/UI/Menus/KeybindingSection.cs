using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Keybinding list of the SettingsMenu: one row per InputBindingSettings entry,
// interactive rebinding with conflict detection, and per-action input modes.
public class KeybindingSection
{
    private const float RowHeight = 28f;

    private static readonly string[] ModeLabels = { "Press", "Hold", "Toggle", "Dbl" };
    private static readonly Color ModeSelectedColor   = new Color(0.3f, 0.55f, 1f, 0.95f);
    private static readonly Color ModeUnselectedColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);

    private readonly PlayerInputHandler   _input;
    private readonly InputBindingSettings _bindings;

    private GameObject _rebindOverlay;
    private TextMeshProUGUI _conflictText;

    private TextMeshProUGUI[] _primaryLabels;
    private TextMeshProUGUI[] _secondaryLabels;
    private Image[][] _modeButtonImages;

    // Rebind state: -1 = not listening
    private int  _listeningAction = -1;
    private bool _listeningPrimary;
    private int  _listenStartFrame;
    private bool _rebindStarted;
    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

    // Transient warning shown when a rebind attempt conflicts with an existing binding
    private string _conflictMessage;
    private float  _conflictMessageTimer;

    public bool IsListening => _listeningAction >= 0;

    public KeybindingSection(RectTransform window, RectTransform overlayParent,
        PlayerInputHandler input, InputBindingSettings bindings, float windowWidth)
    {
        _input    = input;
        _bindings = bindings;
        Build(window, windowWidth);
        BuildRebindOverlay(overlayParent);
    }

    public void Tick(float unscaledDeltaTime)
    {
        // Defer starting the interactive rebind by one frame so the mouse click that
        // opened the listening UI isn't immediately captured as the new binding.
        if (IsListening && !_rebindStarted && Time.frameCount > _listenStartFrame)
        {
            _rebindStarted = true;
            BeginRebind((GameAction)_listeningAction, _listeningPrimary);
        }

        _rebindOverlay.SetActive(IsListening);

        if (_conflictMessageTimer > 0f)
        {
            _conflictMessageTimer -= unscaledDeltaTime;
            _conflictText.gameObject.SetActive(true);
            _conflictText.text = _conflictMessage;
        }
        else
        {
            _conflictText.gameObject.SetActive(false);
        }
    }

    public void CancelRebind()
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

    public void ResetToDefaults()
    {
        CancelRebind();
        _bindings.ResetToDefaults();
        _input.RebuildActions();
        SettingsSave.DeleteBindings();
        RefreshAllRows();
    }

    // ── Rebind state machine ────────────────────────────────────────────────

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

    // ── UI construction ─────────────────────────────────────────────────────

    private void Build(RectTransform window, float windowWidth)
    {
        var header = UIFactory.MakeText("KeybindingsHeader", window);
        header.text = "── Keybindings ──";
        header.fontSize = 13f;
        UIFactory.Place(header.rectTransform, new Vector2(10f, -132f), new Vector2(580f, 20f));

        MakeColumnHeader(window, "Action",    10f,  135f);
        MakeColumnHeader(window, "Primary",   150f, 120f);
        MakeColumnHeader(window, "Secondary", 275f, 120f);
        MakeColumnHeader(window, "Mode",      400f, 200f);

        int count = _bindings.Bindings.Count;
        _primaryLabels     = new TextMeshProUGUI[count];
        _secondaryLabels   = new TextMeshProUGUI[count];
        _modeButtonImages  = new Image[count][];

        float contentHeight = count * RowHeight;
        var content = BuildScrollView(window, new Vector2(10f, -174f), new Vector2(windowWidth - 20f, 310f),
            contentHeight, windowWidth - 40f);

        for (int i = 0; i < count; i++)
            BuildBindingRow(i, content, i * RowHeight);
    }

    private static void MakeColumnHeader(RectTransform window, string label, float x, float width)
    {
        var text = UIFactory.MakeText("Header_" + label, window);
        text.text = label;
        text.fontSize = 12f;
        UIFactory.Place(text.rectTransform, new Vector2(x, -154f), new Vector2(width, 20f));
    }

    private static RectTransform BuildScrollView(RectTransform parent, Vector2 pos, Vector2 size, float contentHeight, float contentWidth)
    {
        var scrollGO = new GameObject("BindingsScroll", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollGO.transform.SetParent(parent, false);
        var scrollRt = scrollGO.GetComponent<RectTransform>();
        UIFactory.Place(scrollRt, pos, size);

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

        var label = UIFactory.MakeText("Action_" + rowIndex, content);
        label.text = b.Action.ToString();
        label.fontSize = 12f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        UIFactory.Place(label.rectTransform, new Vector2(0f, -rowY), new Vector2(140f, 24f));

        var primaryBtn = UIFactory.MakeButton("Primary_" + rowIndex, content, "", out var primaryLabel);
        UIFactory.Place(primaryBtn.GetComponent<RectTransform>(), new Vector2(145f, -rowY), new Vector2(120f, 24f));
        primaryBtn.onClick.AddListener(() => StartListening(b.Action, true));
        _primaryLabels[rowIndex] = primaryLabel;

        var secondaryBtn = UIFactory.MakeButton("Secondary_" + rowIndex, content, "", out var secondaryLabel);
        UIFactory.Place(secondaryBtn.GetComponent<RectTransform>(), new Vector2(270f, -rowY), new Vector2(120f, 24f));
        secondaryBtn.onClick.AddListener(() => StartListening(b.Action, false));
        _secondaryLabels[rowIndex] = secondaryLabel;

        var modeImages = new Image[ModeLabels.Length];
        float modeBtnWidth = 195f / ModeLabels.Length;
        for (int m = 0; m < ModeLabels.Length; m++)
        {
            int modeIndex = m;
            var modeBtn = UIFactory.MakeButton("Mode_" + rowIndex + "_" + m, content, ModeLabels[m], out var modeLabel);
            modeLabel.fontSize = 10f;
            UIFactory.Place(modeBtn.GetComponent<RectTransform>(),
                new Vector2(395f + m * modeBtnWidth, -rowY), new Vector2(modeBtnWidth - 2f, 24f));
            modeBtn.onClick.AddListener(() => OnModeClicked(rowIndex, modeIndex));
            modeImages[m] = (Image)modeBtn.targetGraphic;
        }
        _modeButtonImages[rowIndex] = modeImages;

        RefreshRow(rowIndex);
    }

    private void BuildRebindOverlay(RectTransform parent)
    {
        var overlayGO = new GameObject("RebindOverlay", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parent, false);
        _rebindOverlay = overlayGO;

        var overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImg.raycastTarget = true; // block clicks to the window while listening
        UIFactory.Stretch(overlayGO.GetComponent<RectTransform>());

        var promptText = UIFactory.MakeText("Prompt", overlayGO.GetComponent<RectTransform>());
        promptText.text = "Press any key, mouse, or gamepad button\n(Escape to cancel)";
        promptText.fontSize = 16f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        promptText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        promptText.rectTransform.anchoredPosition = new Vector2(0f, 25f);
        promptText.rectTransform.sizeDelta = new Vector2(400f, 50f);

        _conflictText = UIFactory.MakeText("ConflictMessage", overlayGO.GetComponent<RectTransform>());
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
