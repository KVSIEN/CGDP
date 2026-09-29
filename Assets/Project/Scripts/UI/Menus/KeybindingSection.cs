using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CGD.Input;

namespace CGD.UI
{
    // Keybinding list of the SettingsMenu: one row per InputBindingSettings entry,
    // interactive rebinding with conflict detection, and per-action input modes.
    public class KeybindingSection
    {
        private const float RowHeight = 28f;
        private const float EscapeHoldToClear = 0.6f;

        private static readonly string[] ModeLabels = { "Press", "Hold", "Toggle", "Dbl" };
        private static readonly Color ModeSelectedColor   = new Color(0.3f, 0.55f, 1f, 0.95f);
        private static readonly Color ModeUnselectedColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly Color SlotNormalColor  = new Color(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly Color SlotSharedColor  = new Color(0.65f, 0.32f, 0.05f, 0.95f);
        private static readonly Color SharedTextColor  = new Color(1f, 0.75f, 0.35f, 1f);

        private readonly PlayerInputHandler   _input;
        private readonly InputBindingSettings _bindings;

        private GameObject _rebindOverlay;
        private TextMeshProUGUI _conflictText;
        private TextMeshProUGUI _promptText;

        private TextMeshProUGUI[] _primaryLabels;
        private TextMeshProUGUI[] _secondaryLabels;
        private Image[] _primaryImages;
        private Image[] _secondaryImages;
        private Image[][] _modeButtonImages;

        // Rebind state: -1 = not listening
        private int  _listeningAction = -1;
        private bool _listeningPrimary;
        private int  _listenStartFrame;
        private bool _rebindStarted;
        private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

        // Transient warning shown when a rebind attempt conflicts with an existing binding
        private float  _escapeHeldTime;
        private bool   _escapeHoldHandled;

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

            TickEscape(unscaledDeltaTime);
            // Stays up while a conflict warning is showing, with the prompt swapped for it.
            _rebindOverlay.SetActive(IsListening || _conflictMessageTimer > 0f);
            _promptText.gameObject.SetActive(IsListening);

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

        // Tap Escape = leave the slot as it was; hold Escape = unbind it.
        private void TickEscape(float unscaledDeltaTime)
        {
            var escape = Keyboard.current?.escapeKey;
            if (!IsListening || escape == null)
            {
                _escapeHeldTime    = 0f;
                _escapeHoldHandled = false;
                return;
            }

            if (escape.wasPressedThisFrame && _escapeHeldTime <= 0f) _escapeHeldTime = 0.0001f;

            if (escape.isPressed)
            {
                if (_escapeHoldHandled) return;

                _escapeHeldTime += unscaledDeltaTime;
                if (_escapeHeldTime < EscapeHoldToClear) return;

                _escapeHoldHandled = true;
                ClearListeningSlot();
                return;
            }

            if (_escapeHeldTime > 0f) CancelRebind();
            _escapeHeldTime    = 0f;
            _escapeHoldHandled = false;
        }

        private void ClearListeningSlot()
        {
            var action  = (GameAction)_listeningAction;
            bool primary = _listeningPrimary;
            CancelRebind();
            WriteRebind(action, primary, string.Empty);
        }

        public void CancelRebind()
        {
            int prevAction = _listeningAction;
            bool rebindWasRunning = _rebindStarted;

            _rebindOperation?.Cancel();
            _rebindOperation      = null;
            _listeningAction      = -1;
            _rebindStarted        = false;
            _conflictMessageTimer = 0f;

            if (rebindWasRunning) _input.RebuildActions(); // re-enable the action and drop the transient override

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

        private void BeginRebind(GameAction action, bool primary)
        {
            var inputAction = _input.GetInputAction(action);
            // Unity refuses to rebind an enabled action; RebuildActions() re-enables it afterwards.
            inputAction.Disable();
            int bindingIndex = primary ? 0 : 1;

            _rebindOperation = inputAction.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Keyboard>/escape")
                .WithControlsExcluding("<Keyboard>/anyKey") // would also match the Escape press used to cancel/clear
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

            // Sharing a control between actions is allowed; the player is warned and both slots
            // stay highlighted in the list until one of them is changed.
            string sharedWith = FindSharedActions(action, primary, path);
            WriteRebind(action, primary, path);

            if (sharedWith.Length == 0) return;
            _conflictMessage      = $"{BindingLabel(path)} is also bound to {sharedWith}";
            _conflictMessageTimer = 3f;
        }

        // Comma-separated actions that use this control path in a slot other than the one
        // given (so a slot is never shared with itself); empty when the path is unshared.
        private string FindSharedActions(GameAction action, bool primary, string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            var names = new System.Collections.Generic.List<string>();
            foreach (var b in _bindings.Bindings)
            {
                bool isOwnPrimarySlot   = b.Action == action && primary;
                bool isOwnSecondarySlot = b.Action == action && !primary;

                bool shared = (!isOwnPrimarySlot && b.PrimaryPath == path)
                           || (!isOwnSecondarySlot && b.SecondaryPath == path);
                if (shared) names.Add(b.Action.ToString());
            }
            return string.Join(", ", names);
        }

        private bool IsShared(GameAction action, bool primary, string path) =>
            FindSharedActions(action, primary, path).Length > 0;

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

            RefreshAllRows();
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
            _primaryImages     = new Image[count];
            _secondaryImages   = new Image[count];
            _modeButtonImages  = new Image[count][];

            float contentHeight = count * RowHeight;
            var content = BuildScrollView(window, new Vector2(10f, -174f), new Vector2(windowWidth - 20f, 310f),
                contentHeight, windowWidth - 40f);

            for (int i = 0; i < count; i++)
                BuildBindingRow(i, content, i * RowHeight);

            RefreshAllRows(); // rows built earlier can only see sharing with later rows now
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
            _primaryImages[rowIndex] = (Image)primaryBtn.targetGraphic;

            var secondaryBtn = UIFactory.MakeButton("Secondary_" + rowIndex, content, "", out var secondaryLabel);
            UIFactory.Place(secondaryBtn.GetComponent<RectTransform>(), new Vector2(270f, -rowY), new Vector2(120f, 24f));
            secondaryBtn.onClick.AddListener(() => StartListening(b.Action, false));
            _secondaryLabels[rowIndex] = secondaryLabel;
            _secondaryImages[rowIndex] = (Image)secondaryBtn.targetGraphic;

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
            overlayImg.color = new Color(0f, 0f, 0f, 0.8f);
            overlayImg.raycastTarget = true; // block clicks to the window while listening
            var overlayRt = overlayGO.GetComponent<RectTransform>();
            UIFactory.Stretch(overlayRt);

            // Opaque card so the prompt never blends into the keybinding rows behind it.
            var card = UIFactory.MakeImage("PromptCard", overlayRt);
            card.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            var cardRt = card.rectTransform;
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(440f, 130f);

            var promptText = UIFactory.MakeText("Prompt", cardRt);
            _promptText = promptText;
            promptText.text = "Press any key, mouse or gamepad button\n<size=13><color=#9AA0B0>Tap Escape to cancel  ·  Hold Escape to clear the binding</color></size>";
            promptText.fontSize = 17f;
            promptText.color = Color.white;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            promptText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            promptText.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            promptText.rectTransform.sizeDelta = new Vector2(410f, 60f);

            _conflictText = UIFactory.MakeText("ConflictMessage", cardRt);
            _conflictText.fontSize = 14f;
            _conflictText.color = SharedTextColor;
            _conflictText.alignment = TextAlignmentOptions.Center;
            _conflictText.rectTransform.anchorMin = _conflictText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _conflictText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _conflictText.rectTransform.anchoredPosition = new Vector2(0f, -38f);
            _conflictText.rectTransform.sizeDelta = new Vector2(410f, 30f);

            _rebindOverlay.SetActive(false);
        }

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

            // Controls shared with another action are shown in orange in both places they appear.
            ApplySlotStyle(rowIndex, true,  IsShared(b.Action, true,  b.PrimaryPath));
            ApplySlotStyle(rowIndex, false, IsShared(b.Action, false, b.SecondaryPath));

            var images = _modeButtonImages[rowIndex];
            for (int m = 0; m < images.Length; m++)
                images[m].color = (int)b.Mode == m ? ModeSelectedColor : ModeUnselectedColor;
        }

        private void ApplySlotStyle(int rowIndex, bool primary, bool shared)
        {
            var image = primary ? _primaryImages[rowIndex] : _secondaryImages[rowIndex];
            var label = primary ? _primaryLabels[rowIndex] : _secondaryLabels[rowIndex];
            if (image == null || label == null) return;

            image.color = shared ? SlotSharedColor : SlotNormalColor;
            label.color = shared ? SharedTextColor : Color.white;
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
}
