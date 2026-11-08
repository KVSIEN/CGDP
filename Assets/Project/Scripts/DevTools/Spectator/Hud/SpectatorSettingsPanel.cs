using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.UI;

namespace CGD.DevTools
{
    // The spectator's settings (Escape): how the camera feels — look sensitivity, invert,
    // move speed, smoothing, label size — and every spectator control, rebindable in two
    // slots like the player's keybinding menu (SpectatorRebinder), with controls used twice
    // shown in orange. Preferences apply as they change and are saved when it closes.
    internal sealed class SpectatorSettingsPanel
    {
        private const float WindowWidth  = 720f;
        private const float WindowHeight = 660f;
        private const float RowHeight    = 28f;
        private const float LabelWidth   = 210f;
        private const float ControlX     = 240f;
        private const float ControlWidth = 440f;

        private static readonly Color WindowColor    = new(0.07f, 0.08f, 0.12f, 0.97f);
        private static readonly Color SlotColor      = new(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly Color SlotSharedColor = new(0.65f, 0.32f, 0.05f, 0.95f);
        private static readonly Color SharedTextColor = new(1f, 0.75f, 0.35f, 1f);
        private static readonly Color MutedText      = new(0.6f, 0.63f, 0.7f, 1f);

        private sealed class Row
        {
            public SpectatorAction  Action;
            public TextMeshProUGUI  Primary;
            public TextMeshProUGUI  Secondary;
            public Image            PrimarySlot;
            public Image            SecondarySlot;
        }

        private readonly SpectatorInputSettings _settings;
        private readonly SpectatorPreferences   _preferences;
        private readonly SpectatorRebinder      _rebinder;
        private readonly Action                 _preferencesChanged;
        private readonly GameObject             _root;
        private readonly List<Row>              _rows = new();

        private LabeledSlider _sensitivity;
        private OptionCycler  _invert;
        private LabeledSlider _speed;
        private LabeledSlider _smoothing;
        private LabeledSlider _labelSize;
        private GameObject      _overlay;
        private TextMeshProUGUI _prompt;
        private TextMeshProUGUI _message;

        public SpectatorSettingsPanel(RectTransform canvas, SpectatorInputSettings settings, SpectatorInput input,
                                      SpectatorPreferences preferences, Action preferencesChanged)
        {
            _settings           = settings;
            _preferences        = preferences;
            _preferencesChanged = preferencesChanged;
            _rebinder           = new SpectatorRebinder(input, settings);
            _rebinder.Changed  += RefreshRows;

            // Dims the view and catches clicks, so nothing behind the window is picked.
            Image backdrop = UIFactory.MakeImage("Spectator Settings", canvas);
            backdrop.color = new Color(0f, 0f, 0f, 0.45f);
            backdrop.raycastTarget = true;
            UIFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;

            Image window = UIFactory.MakeImage("Window", backdrop.rectTransform);
            window.color = WindowColor;
            window.raycastTarget = true;
            RectTransform rect = window.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(WindowWidth, WindowHeight);

            BuildView(rect);
            BuildControls(rect);
            BuildButtons(rect);
            BuildOverlay(rect);
            _root.SetActive(false);
        }

        public bool IsOpen      => _root.activeSelf;
        public bool IsRebinding => _rebinder.IsListening;

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            RefreshValues();
            RefreshRows();
            _root.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _rebinder.Cancel();
            _root.SetActive(false);
            SpectatorSave.SavePreferences(_preferences);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!IsOpen) return;
            _rebinder.Tick(unscaledDeltaTime);

            bool message = _rebinder.HasMessage;
            _overlay.SetActive(_rebinder.IsListening || message);
            _prompt.gameObject.SetActive(_rebinder.IsListening);
            _message.gameObject.SetActive(message);
            if (message) _message.text = _rebinder.Message;
        }

        // --- Building -----------------------------------------------------------------

        private void BuildView(RectTransform window)
        {
            TextMeshProUGUI title = MakeLabel("Title", window, "Spectator settings", 22f, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(title.rectTransform, new Vector2(20f, -14f), new Vector2(400f, 30f));
            TextMeshProUGUI note = MakeLabel("Note", window, "Changes apply at once and are kept for next time", 13f, TextAlignmentOptions.MidlineRight);
            note.color = MutedText;
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
            TextMeshProUGUI header = MakeLabel("Controls", window, "── Controls ──   click a slot, then press the new key or button", 13f, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(header.rectTransform, new Vector2(20f, Top), new Vector2(680f, 20f));
            Column(window, "Action", 20f, Top - 22f);
            Column(window, "Primary", ControlX, Top - 22f);
            Column(window, "Secondary", ControlX + 225f, Top - 22f);

            int count = _settings.Bindings.Count;
            RectTransform content = ScrollView(window, new Vector2(20f, Top - 44f), new Vector2(WindowWidth - 40f, 300f), count * RowHeight);
            for (int i = 0; i < count; i++) BuildRow(content, _settings.Bindings[i].Action, -i * RowHeight);
        }

        private void BuildRow(RectTransform content, SpectatorAction action, float y)
        {
            TextMeshProUGUI label = MakeLabel("Action " + action, content, SpectatorActionLabels.Of(action), 13f, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(label.rectTransform, new Vector2(0f, y), new Vector2(LabelWidth, 24f));

            var row = new Row { Action = action };
            bool fixedRow = SpectatorInputSettings.IsFixed(action);
            row.PrimarySlot   = Slot(content, action, true,  new Vector2(ControlX - 20f, y), fixedRow, out row.Primary);
            row.SecondarySlot = Slot(content, action, false, new Vector2(ControlX + 205f, y), fixedRow, out row.Secondary);
            _rows.Add(row);
        }

        private Image Slot(RectTransform content, SpectatorAction action, bool primary, Vector2 position, bool fixedRow, out TextMeshProUGUI label)
        {
            Button button = UIFactory.MakeButton((primary ? "Primary " : "Secondary ") + action, content, "", out label);
            label.fontSize = 13f;
            UIFactory.Place(button.GetComponent<RectTransform>(), position, new Vector2(215f, 24f));
            button.interactable = !fixedRow;
            if (!fixedRow) button.onClick.AddListener(() => _rebinder.StartListening(action, primary));
            return (Image)button.targetGraphic;
        }

        private void BuildButtons(RectTransform window)
        {
            float y = -WindowHeight + 52f;
            AddButton(window, "Reset controls", new Vector2(20f, y), () => _rebinder.ResetToDefaults());
            AddButton(window, "Reset view settings", new Vector2(220f, y), () =>
            {
                _preferences.ResetView(_settings);
                RefreshValues();
                Changed();
            });
            AddButton(window, "Close   (Esc)", new Vector2(WindowWidth - 200f, y), Close);
        }

        private void BuildOverlay(RectTransform window)
        {
            Image overlay = UIFactory.MakeImage("Rebind Overlay", window);
            overlay.color = new Color(0f, 0f, 0f, 0.8f);
            overlay.raycastTarget = true;   // no clicks on the list while it listens
            UIFactory.Stretch(overlay.rectTransform);
            _overlay = overlay.gameObject;

            Image card = UIFactory.MakeImage("Card", overlay.rectTransform);
            card.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            card.rectTransform.anchorMin = card.rectTransform.anchorMax = card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.rectTransform.sizeDelta = new Vector2(460f, 130f);

            _prompt = MakeLabel("Prompt", card.rectTransform,
                "Press any key, mouse or gamepad button\n<size=13><color=#9AA0B0>Tap Escape to cancel  ·  Hold Escape to clear the slot</color></size>",
                17f, TextAlignmentOptions.Center);
            UIFactory.Stretch(_prompt.rectTransform);

            _message = MakeLabel("Message", card.rectTransform, "", 15f, TextAlignmentOptions.Center);
            _message.color = SharedTextColor;
            UIFactory.Stretch(_message.rectTransform);
            _overlay.SetActive(false);
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

        private void RefreshRows()
        {
            foreach (Row row in _rows)
            {
                if (!_settings.TryGet(row.Action, out SpectatorBinding binding)) continue;
                Refresh(row.Primary, row.PrimarySlot, row.Action, true, binding.PrimaryPath);
                Refresh(row.Secondary, row.SecondarySlot, row.Action, false, binding.SecondaryPath);
            }
        }

        private void Refresh(TextMeshProUGUI label, Image slot, SpectatorAction action, bool primary, string path)
        {
            bool listening = _rebinder.IsListeningTo(action, primary);
            bool shared    = !listening && _rebinder.SharedWith(action, primary, path).Length > 0;
            label.text  = listening ? "▪▪▪" : SpectatorRebinder.Label(path);
            label.color = shared ? SharedTextColor : Color.white;
            slot.color  = shared ? SlotSharedColor : SlotColor;
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
            TextMeshProUGUI text = MakeLabel("Label " + label, window, label, 14f, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(text.rectTransform, new Vector2(20f, y), new Vector2(LabelWidth, 22f));
        }

        private static void Column(RectTransform window, string label, float x, float y)
        {
            TextMeshProUGUI text = MakeLabel("Column " + label, window, label, 12f, TextAlignmentOptions.MidlineLeft);
            text.color = MutedText;
            UIFactory.Place(text.rectTransform, new Vector2(x, y), new Vector2(200f, 20f));
        }

        private static void AddButton(RectTransform window, string label, Vector2 position, UnityEngine.Events.UnityAction clicked)
        {
            Button button = UIFactory.MakeButton(label, window, label, out _);
            UIFactory.Place(button.GetComponent<RectTransform>(), position, new Vector2(180f, 32f));
            button.onClick.AddListener(clicked);
        }

        private static TextMeshProUGUI MakeLabel(string name, RectTransform parent, string value, float size, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = UIFactory.MakeText(name, parent);
            text.text      = value;
            text.fontSize  = size;
            text.alignment = alignment;
            return text;
        }

        private static RectTransform ScrollView(RectTransform parent, Vector2 position, Vector2 size, float contentHeight)
        {
            var view = new GameObject("Controls Scroll", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            view.transform.SetParent(parent, false);
            var viewRect = (RectTransform)view.transform;
            UIFactory.Place(viewRect, position, size);
            view.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
            view.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewRect, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = contentRect.anchorMax = contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(size.x - 20f, contentHeight);

            var scroll = view.GetComponent<ScrollRect>();
            scroll.content      = contentRect;
            scroll.viewport     = viewRect;
            scroll.horizontal   = false;
            scroll.vertical     = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;
            return contentRect;
        }
    }
}
