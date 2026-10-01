using System;
using System.Collections.Generic;
using CGD.Input;
using CGD.UI;

namespace CGD.Player
{
    // Reads a row of slot keys (weapons 1–4, items 5–8) and tells a press, a tap and a
    // hold apart. Holding a key past HoldTime opens the selection wheel for that slot; the
    // camera stops turning, the look input picks a choice and releasing the key confirms
    // it (releasing in the middle cancels). Without a wheel, holding does nothing extra.
    public class SlotKeyWheel
    {
        public const float HoldTime = 0.25f;

        private readonly PlayerInputHandler _input;
        private readonly SlotWheelHUD _view;
        private readonly GameAction[] _keys;
        private readonly string _title;
        private readonly RadialSelection _selection = new();

        private int _heldKey = -1;
        private float _heldFor;
        private bool _wheelOpen;

        // title: shown in the wheel's centre with the slot number ("Weapon" → "Weapon 2").
        public SlotKeyWheel(PlayerInputHandler input, SlotWheelHUD view, GameAction[] keys, string title)
        {
            _input = input;
            _view  = view;
            _keys  = keys;
            _title = title;
        }

        public bool IsOpen => _wheelOpen;

        // pressed(slot): the key went down. tapped(slot): released before HoldTime.
        // options(slot): the wheel's choices. chosen(slot, option): picked from the wheel.
        public void Tick(float dt, Action<int> pressed, Action<int> tapped,
            Func<int, IReadOnlyList<string>> options, Action<int, int> chosen)
        {
            // A menu took over input mid-hold: drop it rather than treat it as a release.
            if (!_input.InputEnabled)
            {
                Cancel();
                return;
            }

            if (_heldKey < 0)
            {
                for (int i = 0; i < _keys.Length; i++)
                {
                    if (!_input.WasPressed(_keys[i])) continue;
                    _heldKey = i;
                    _heldFor = 0f;
                    pressed?.Invoke(i);
                    return;
                }
                return;
            }

            int slot = _heldKey;
            if (!_input.IsHeld(_keys[slot]))
            {
                if (_wheelOpen)
                {
                    int picked = _selection.Selected;
                    Close();
                    if (picked >= 0) chosen?.Invoke(slot, picked);
                }
                else tapped?.Invoke(slot);
                _heldKey = -1;
                return;
            }

            _heldFor += dt;
            if (!_wheelOpen && _heldFor >= HoldTime && _view != null && options != null)
            {
                IReadOnlyList<string> choices = options(slot);
                if (choices.Count == 0) return;
                _selection.Reset(choices.Count);
                _view.Open($"{_title} {slot + 1}", choices);
                _input.LookBlocked = true;
                _wheelOpen = true;
            }

            if (!_wheelOpen) return;
            _selection.Feed(_input.WheelLook, _input.IsGamepadLook);
            _view.Highlight(_selection.Selected);
        }

        // Closes the wheel without choosing (death, input locked by a menu).
        public void Cancel()
        {
            if (_wheelOpen) Close();
            _heldKey = -1;
        }

        private void Close()
        {
            _wheelOpen = false;
            _input.LookBlocked = false;
            if (_view != null) _view.Hide();
        }
    }
}
