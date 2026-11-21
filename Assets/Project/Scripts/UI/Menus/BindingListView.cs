using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using CGD.Input;

namespace CGD.UI
{
    // The rebindable half of a binding menu (the player's keybinding page, the spectator's
    // settings): the slot buttons, which show each control, "▪▪▪" while one listens and orange
    // where a control is used twice, and the "press a key" card over the window. Owns the
    // BindingRebinder that drives them. The menu lays out its own rows and adds a slot per
    // action and side; anything else on a row (input modes) it refreshes on Changed.
    public sealed class BindingListView<TAction> where TAction : struct, Enum
    {
        private static readonly Color SlotColor       = new(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly Color SlotSharedColor = new(0.65f, 0.32f, 0.05f, 0.95f);
        public  static readonly Color SharedTextColor = new(1f, 0.75f, 0.35f, 1f);

        private const string Prompt =
            "Press any key, mouse or gamepad button\n<size=13><color=#9AA0B0>Tap Escape to cancel  ·  Hold Escape to clear the slot</color></size>";

        private struct Slot
        {
            public TAction         Action;
            public bool            Primary;
            public TextMeshProUGUI Label;
            public Image           Background;
        }

        private readonly BindingTable<TAction>    _table;
        private readonly BindingRebinder<TAction> _rebinder;
        private readonly bool _omitDevice;
        private readonly List<Slot> _slots = new();

        private GameObject      _overlay;
        private TextMeshProUGUI _prompt;
        private TextMeshProUGUI _message;

        // omitDevice: show "Space" rather than "Space [Keyboard]".
        public BindingListView(BindingTable<TAction> table, Func<TAction, InputAction> actionOf, Action rebuild, bool omitDevice)
        {
            _table      = table;
            _omitDevice = omitDevice;
            _rebinder   = new BindingRebinder<TAction>(table, actionOf, rebuild);
            _rebinder.Changed += Refresh;
        }

        public bool IsListening => _rebinder.IsListening;

        // After every slot has redrawn: bindings or the listening slot changed.
        public event Action Changed;

        public void Cancel() => _rebinder.Cancel();

        public void ResetToDefaults() => _rebinder.ResetToDefaults();

        // One side of one action's controls, as a button that listens for a new control when
        // clicked (unless the table keeps that row fixed). Returns its label for sizing.
        public TextMeshProUGUI AddSlot(RectTransform parent, TAction action, bool primary, Vector2 position, Vector2 size)
        {
            Button button = UIFactory.MakeButton((primary ? "Primary " : "Secondary ") + action, parent, "", out TextMeshProUGUI label);
            UIFactory.Place(button.GetComponent<RectTransform>(), position, size);

            bool rebindable = _table.IsRebindable(action);
            button.interactable = rebindable;
            if (rebindable) button.onClick.AddListener(() => _rebinder.StartListening(action, primary));

            _slots.Add(new Slot { Action = action, Primary = primary, Label = label, Background = (Image)button.targetGraphic });
            return label;
        }

        // The card that asks for the new control, over `parent` (it blocks clicks behind it while up).
        public void BuildOverlay(RectTransform parent, Vector2 cardSize)
        {
            Image overlay = UIFactory.MakeImage("Rebind Overlay", parent);
            overlay.color = new Color(0f, 0f, 0f, 0.8f);
            overlay.raycastTarget = true;
            UIFactory.Stretch(overlay.rectTransform);
            _overlay = overlay.gameObject;

            // Opaque so the prompt never blends into the rows behind it.
            Image card = UIFactory.MakeImage("Card", overlay.rectTransform);
            card.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            card.rectTransform.anchorMin = card.rectTransform.anchorMax = card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.rectTransform.sizeDelta = cardSize;

            _prompt = UIFactory.MakeLabel("Prompt", card.rectTransform, Prompt, 17f, TextAlignmentOptions.Center);
            UIFactory.Stretch(_prompt.rectTransform);

            _message = UIFactory.MakeLabel("Message", card.rectTransform, "", 15f, TextAlignmentOptions.Center);
            _message.color = SharedTextColor;
            UIFactory.Stretch(_message.rectTransform);
            _overlay.SetActive(false);
        }

        public void Tick(float unscaledDeltaTime)
        {
            _rebinder.Tick(unscaledDeltaTime);
            if (_overlay == null) return;

            // Stays up while a shared-control warning shows, with the prompt swapped for it.
            bool message = _rebinder.HasMessage;
            _overlay.SetActive(_rebinder.IsListening || message);
            _prompt.gameObject.SetActive(_rebinder.IsListening);
            _message.gameObject.SetActive(message);
            if (message) _message.text = _rebinder.Message;
        }

        public void Refresh()
        {
            foreach (Slot slot in _slots)
            {
                int index = _table.IndexOf(slot.Action);
                if (index < 0) continue;

                string path    = _table.PathAt(index, slot.Primary);
                bool listening = _rebinder.IsListeningTo(slot.Action, slot.Primary);
                bool shared    = !listening && _rebinder.IsShared(slot.Action, slot.Primary, path);

                slot.Label.text       = listening ? "▪▪▪" : BindingRebinder<TAction>.Label(path, _omitDevice);
                slot.Label.color      = shared ? SharedTextColor : Color.white;
                slot.Background.color = shared ? SlotSharedColor : SlotColor;
            }
            Changed?.Invoke();
        }
    }
}
