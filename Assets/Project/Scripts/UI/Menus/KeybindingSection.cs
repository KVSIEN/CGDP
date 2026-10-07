using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CGD.Input;

namespace CGD.UI
{
    // Keybinding list of the SettingsMenu: one row per InputBindingSettings entry,
    // interactive rebinding with conflict detection, and per-action input modes.
    public class KeybindingSection
    {
        private const float RowHeight = 28f;

        private static readonly string[] ModeLabels = { "Press", "Hold", "Toggle", "Dbl" };
        private static readonly Color ModeSelectedColor   = new Color(0.3f, 0.55f, 1f, 0.95f);
        private static readonly Color ModeUnselectedColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly Color SlotNormalColor  = new Color(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly Color SlotSharedColor  = new Color(0.65f, 0.32f, 0.05f, 0.95f);
        private static readonly Color SharedTextColor  = new Color(1f, 0.75f, 0.35f, 1f);

        private readonly PlayerInputHandler   _input;
        private readonly InputBindingSettings _bindings;
        private readonly KeybindingRebinder   _rebinder;

        private GameObject _rebindOverlay;
        private TextMeshProUGUI _conflictText;
        private TextMeshProUGUI _promptText;

        private TextMeshProUGUI[] _primaryLabels;
        private TextMeshProUGUI[] _secondaryLabels;
        private Image[] _primaryImages;
        private Image[] _secondaryImages;
        private Image[][] _modeButtonImages;

        public bool IsListening => _rebinder.IsListening;

        public KeybindingSection(RectTransform window, RectTransform overlayParent,
            PlayerInputHandler input, InputBindingSettings bindings, float windowWidth)
        {
            _input    = input;
            _bindings = bindings;
            _rebinder = new KeybindingRebinder(input, bindings);
            _rebinder.ActionChanged   += RefreshActionRow;
            _rebinder.BindingsChanged += RefreshAllRows;
            Build(window, windowWidth);
            BuildRebindOverlay(overlayParent);
        }

        public void Tick(float unscaledDeltaTime)
        {
            _rebinder.Tick(unscaledDeltaTime);

            // Stays up while a conflict warning is showing, with the prompt swapped for it.
            bool conflict = _rebinder.HasConflictMessage;
            _rebindOverlay.SetActive(IsListening || conflict);
            _promptText.gameObject.SetActive(IsListening);
            _conflictText.gameObject.SetActive(conflict);
            if (conflict) _conflictText.text = _rebinder.ConflictMessage;
        }

        public void CancelRebind() => _rebinder.CancelRebind();

        public void ResetToDefaults() => _rebinder.ResetToDefaults();

        private void RefreshActionRow(GameAction action)
        {
            int row = RowIndexOf(action);
            if (row >= 0) RefreshRow(row);
        }

        private int RowIndexOf(GameAction action)
        {
            for (int i = 0; i < _bindings.Bindings.Count; i++)
                if (_bindings.Bindings[i].Action == action) return i;
            return -1;
        }

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
            primaryBtn.onClick.AddListener(() => _rebinder.StartListening(b.Action, true));
            _primaryLabels[rowIndex] = primaryLabel;
            _primaryImages[rowIndex] = (Image)primaryBtn.targetGraphic;

            var secondaryBtn = UIFactory.MakeButton("Secondary_" + rowIndex, content, "", out var secondaryLabel);
            UIFactory.Place(secondaryBtn.GetComponent<RectTransform>(), new Vector2(270f, -rowY), new Vector2(120f, 24f));
            secondaryBtn.onClick.AddListener(() => _rebinder.StartListening(b.Action, false));
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
            bool waitPri = _rebinder.IsListeningTo(b.Action, true);
            bool waitSec = _rebinder.IsListeningTo(b.Action, false);

            _primaryLabels[rowIndex].text   = waitPri ? "▪▪▪" : KeybindingRebinder.BindingLabel(b.PrimaryPath);
            _secondaryLabels[rowIndex].text = waitSec ? "▪▪▪" : KeybindingRebinder.BindingLabel(b.SecondaryPath);

            // Controls shared with another action are shown in orange in both places they appear.
            ApplySlotStyle(rowIndex, true,  _rebinder.IsShared(b.Action, true,  b.PrimaryPath));
            ApplySlotStyle(rowIndex, false, _rebinder.IsShared(b.Action, false, b.SecondaryPath));

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
