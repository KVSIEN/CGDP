using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.UI;

namespace CGD.DevTools
{
    // The spectator's settings (Escape): how the camera feels — look sensitivity, invert,
    // move speed, smoothing, label size — and every spectator control, rebindable in two
    // slots through the same BindingListView as the player's keybinding menu. Preferences apply as they change and are saved when it closes.
    internal sealed class SpectatorSettingsPanel
    {
        private const float WindowWidth  = 720f;
        private const float WindowHeight = 660f;
        private const float RowHeight    = 28f;
        private const float LabelWidth   = 210f;
        private const float ControlX     = 240f;
        private const float ControlWidth = 440f;

        private readonly SpectatorInputSettings _settings;
        private readonly SpectatorPreferences   _preferences;
        private readonly BindingListView<SpectatorAction> _slots;
        private readonly Action                 _preferencesChanged;
        private readonly GameObject             _root;

        private LabeledSlider _sensitivity;
        private OptionCycler  _invert;
        private LabeledSlider _speed;
        private LabeledSlider _smoothing;
        private LabeledSlider _labelSize;

        public SpectatorSettingsPanel(RectTransform canvas, SpectatorInputSettings settings, SpectatorInput input,
                                      SpectatorPreferences preferences, Action preferencesChanged)
        {
            _settings           = settings;
            _preferences        = preferences;
            _preferencesChanged = preferencesChanged;
            _slots              = new BindingListView<SpectatorAction>(settings, input.Get, input.Rebuild, omitDevice: true);

            // Dims the view and catches clicks, so nothing behind the window is picked.
            Image backdrop = UIFactory.MakeImage("Spectator Settings", canvas);
            backdrop.color = new Color(0f, 0f, 0f, 0.45f);
            backdrop.raycastTarget = true;
            UIFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;

            RectTransform rect = SpectatorWindow.Centred("Window", backdrop.rectTransform, new Vector2(WindowWidth, WindowHeight));

            BuildView(rect);
            BuildControls(rect);
            BuildButtons(rect);
            _slots.BuildOverlay(rect, new Vector2(460f, 130f));
            _root.SetActive(false);
        }

        public bool IsOpen      => _root.activeSelf;
        public bool IsRebinding => _slots.IsListening;

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            RefreshValues();
            _slots.Refresh();
            _root.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _slots.Cancel();
            _root.SetActive(false);
            SpectatorSave.SavePreferences(_preferences);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!IsOpen) return;
            _slots.Tick(unscaledDeltaTime);
        }

        // --- Building -----------------------------------------------------------------

        private void BuildView(RectTransform window)
        {
            TextMeshProUGUI title = UIFactory.MakeLabel("Title", window, "Spectator settings", 22f, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(title.rectTransform, new Vector2(20f, -14f), new Vector2(400f, 30f));
            TextMeshProUGUI note = UIFactory.MakeLabel("Note", window, "Changes apply at once and are kept for next time", 13f, TextAlignmentOptions.MidlineRight);
            note.color = SpectatorWindow.MutedText;
            UIFactory.Place(note.rectTransform, new Vector2(WindowWidth - 380f, -18f), new Vector2(360f, 24f));

            float y = -58f;
            _sensitivity = AddSlider(window, "Look sensitivity", ref y, 0.02f, 0.5f, v => $"{v:0.00}°",
                                  v => { _preferences.LookSensitivity = v; Changed(); });
            RowLabel(window, "Invert look (up / down)", y);
            _invert = new OptionCycler(window, new Vector2(ControlX, y), 160f, new[] { "Off", "On" },
                                       i => { _preferences.InvertLook = i == 1; Changed(); });
            y -= 32f;
            _speed = AddSlider(window, "Move speed", ref y, 2f, 40f, v => $"{v:0} m/s",
                            v => { _preferences.MoveSpeed = v; Changed(); });
            _smoothing = AddSlider(window, "Camera smoothing", ref y, 0f, 1f, v => $"{v:P0}",
                                v => { _preferences.Smoothing = v; Changed(); });
            _labelSize = AddSlider(window, "Label size", ref y, 0.6f, 1.6f, v => $"{v:0.0}×",
                                v => { _preferences.LabelScale = v; Changed(); });
        }

        private void BuildControls(RectTransform window)
        {
            const float Top = -232f;
            TextMeshProUGUI header = UIFactory.MakeLabel("Controls", window, "── Controls ──   click a slot, then press the new key or button", 13f, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(header.rectTransform, new Vector2(20f, Top), new Vector2(680f, 20f));
            Column(window, "Action", 20f, Top - 22f);
            Column(window, "Primary", ControlX, Top - 22f);
            Column(window, "Secondary", ControlX + 225f, Top - 22f);

            int count = _settings.Bindings.Count;
            RectTransform content = UIFactory.MakeScrollList("Controls Scroll", window, new Vector2(20f, Top - 44f),
                new Vector2(WindowWidth - 40f, 300f), WindowWidth - 60f, count * RowHeight, scrollSensitivity: 20f);
            for (int i = 0; i < count; i++) BuildRow(content, _settings.Bindings[i].Action, -i * RowHeight);
        }

        private void BuildRow(RectTransform content, SpectatorAction action, float y)
        {
            TextMeshProUGUI label = UIFactory.MakeLabel("Action " + action, content, SpectatorActionLabels.Of(action), 13f, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(label.rectTransform, new Vector2(0f, y), new Vector2(LabelWidth, 24f));

            _slots.AddSlot(content, action, true,  new Vector2(ControlX - 20f, y),  new Vector2(215f, 24f)).fontSize = 13f;
            _slots.AddSlot(content, action, false, new Vector2(ControlX + 205f, y), new Vector2(215f, 24f)).fontSize = 13f;
        }

        private void BuildButtons(RectTransform window)
        {
            float y = -WindowHeight + 52f;
            AddButton(window, "Reset controls", new Vector2(20f, y), () => _slots.ResetToDefaults());
            AddButton(window, "Reset view settings", new Vector2(220f, y), () =>
            {
                _preferences.ResetView(_settings);
                RefreshValues();
                Changed();
            });
            AddButton(window, "Close   (Esc)", new Vector2(WindowWidth - 200f, y), Close);
        }

        // --- Refreshing ---------------------------------------------------------------

        private void RefreshValues()
        {
            _sensitivity.SetValueWithoutNotify(_preferences.LookSensitivity);
            _invert.SetIndexWithoutNotify(_preferences.InvertLook ? 1 : 0);
            _speed.SetValueWithoutNotify(_preferences.MoveSpeed);
            _smoothing.SetValueWithoutNotify(_preferences.Smoothing);
            _labelSize.SetValueWithoutNotify(_preferences.LabelScale);
        }

        private void Changed() => _preferencesChanged?.Invoke();

        // --- Widgets ------------------------------------------------------------------

        private static LabeledSlider AddSlider(RectTransform window, string label, ref float y, float min, float max,
                                            Func<float, string> format, Action<float> changed)
        {
            RowLabel(window, label, y);
            var slider = new LabeledSlider(window, new Vector2(ControlX, y), ControlWidth, min, max, format, changed);
            y -= 32f;
            return slider;
        }

        private static void RowLabel(RectTransform window, string label, float y)
        {
            TextMeshProUGUI text = UIFactory.MakeLabel("Label " + label, window, label, 14f, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(text.rectTransform, new Vector2(20f, y), new Vector2(LabelWidth, 22f));
        }

        private static void Column(RectTransform window, string label, float x, float y)
        {
            TextMeshProUGUI text = UIFactory.MakeLabel("Column " + label, window, label, 12f, TextAlignmentOptions.MidlineLeft);
            text.color = SpectatorWindow.MutedText;
            UIFactory.Place(text.rectTransform, new Vector2(x, y), new Vector2(200f, 20f));
        }

        private static void AddButton(RectTransform window, string label, Vector2 position, UnityEngine.Events.UnityAction clicked)
        {
            Button button = UIFactory.MakeButton(label, window, label, out _);
            UIFactory.Place(button.GetComponent<RectTransform>(), position, new Vector2(180f, 32f));
            button.onClick.AddListener(clicked);
        }
    }
}
