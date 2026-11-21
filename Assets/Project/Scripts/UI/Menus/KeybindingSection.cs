using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CGD.Input;

namespace CGD.UI
{
    // Keybinding list of the SettingsMenu: one row per InputBindingSettings entry, two
    // rebindable slots each (BindingListView) and per-action input modes.
    public class KeybindingSection
    {
        private const float RowHeight = 28f;

        private static readonly string[] ModeLabels = { "Press", "Hold", "Toggle", "Dbl" };
        private static readonly Color ModeSelectedColor   = new Color(0.3f, 0.55f, 1f, 0.95f);
        private static readonly Color ModeUnselectedColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);

        private readonly PlayerInputHandler   _input;
        private readonly InputBindingSettings _bindings;
        private readonly BindingListView<GameAction> _slots;

        private Image[][] _modeButtonImages;

        public bool IsListening => _slots.IsListening;

        public KeybindingSection(RectTransform window, RectTransform overlayParent,
            PlayerInputHandler input, InputBindingSettings bindings, float windowWidth)
        {
            _input    = input;
            _bindings = bindings;
            _slots    = new BindingListView<GameAction>(bindings, input.GetInputAction, input.RebuildActions, omitDevice: false);
            _slots.Changed += RefreshModes;
            Build(window, windowWidth);
            // Over the whole window, not just this page.
            _slots.BuildOverlay(overlayParent, new Vector2(440f, 130f));
        }

        public void Tick(float unscaledDeltaTime) => _slots.Tick(unscaledDeltaTime);

        public void CancelRebind() => _slots.Cancel();

        public void ResetToDefaults() => _slots.ResetToDefaults();

        private void Build(RectTransform window, float windowWidth)
        {
            var header = UIFactory.MakeLabel("KeybindingsHeader", window, "── Keybindings ──", 13f);
            UIFactory.Place(header.rectTransform, new Vector2(10f, -132f), new Vector2(580f, 20f));

            MakeColumnHeader(window, "Action",    10f,  135f);
            MakeColumnHeader(window, "Primary",   150f, 120f);
            MakeColumnHeader(window, "Secondary", 275f, 120f);
            MakeColumnHeader(window, "Mode",      400f, 200f);

            int count = _bindings.Bindings.Count;
            _modeButtonImages = new Image[count][];

            var content = UIFactory.MakeScrollList("BindingsScroll", window, new Vector2(10f, -174f), new Vector2(windowWidth - 20f, 310f),
                windowWidth - 40f, count * RowHeight);

            for (int i = 0; i < count; i++)
                BuildBindingRow(i, content, i * RowHeight);

            _slots.Refresh(); // rows built earlier can only see sharing with later rows now
        }

        private static void MakeColumnHeader(RectTransform window, string label, float x, float width)
        {
            var text = UIFactory.MakeLabel("Header_" + label, window, label, 12f);
            UIFactory.Place(text.rectTransform, new Vector2(x, -154f), new Vector2(width, 20f));
        }

        private void BuildBindingRow(int rowIndex, RectTransform content, float rowY)
        {
            GameAction action = _bindings.ActionAt(rowIndex);

            var label = UIFactory.MakeLabel("Action_" + rowIndex, content, action.ToString(), 12f, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(label.rectTransform, new Vector2(0f, -rowY), new Vector2(140f, 24f));

            _slots.AddSlot(content, action, true,  new Vector2(145f, -rowY), new Vector2(120f, 24f));
            _slots.AddSlot(content, action, false, new Vector2(270f, -rowY), new Vector2(120f, 24f));

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
        }

        private void RefreshModes()
        {
            for (int row = 0; row < _modeButtonImages.Length; row++)
                RefreshMode(row);
        }

        private void RefreshMode(int rowIndex)
        {
            Image[] images = _modeButtonImages[rowIndex];
            if (images == null) return;

            int mode = (int)_bindings.Bindings[rowIndex].Mode;
            for (int m = 0; m < images.Length; m++)
                images[m].color = mode == m ? ModeSelectedColor : ModeUnselectedColor;
        }

        private void OnModeClicked(int rowIndex, int modeIndex)
        {
            var mode = (InputActionMode)modeIndex;
            _bindings.SetMode(rowIndex, mode);
            _input.SetMode(_bindings.ActionAt(rowIndex), mode);
            _bindings.Save();
            RefreshMode(rowIndex);
        }
    }
}
