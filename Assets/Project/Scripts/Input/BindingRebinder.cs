using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CGD.Input
{
    // The rebinding flow behind a binding menu (the player's keybinding page, the spectator's
    // settings): click a slot, press the new control; tap Escape to leave the slot as it was,
    // hold it to clear the slot. A control another action already uses is allowed but called
    // out for a few seconds. Knows nothing about the UI: it raises Changed so the rows redraw.
    public sealed class BindingRebinder<TAction> where TAction : struct, Enum
    {
        private const float EscapeHoldToClear      = 0.6f;
        private const float ConflictMessageSeconds = 3f;

        private readonly BindingTable<TAction>      _table;
        private readonly Func<TAction, InputAction> _actionOf;
        private readonly Action                     _rebuild;

        private bool    _listening;
        private TAction _action;
        private bool    _primary;
        private int     _startFrame;
        private bool    _started;
        private InputActionRebindingExtensions.RebindingOperation _operation;
        private float _escapeHeld;
        private bool  _escapeHandled;
        private float _messageTimer;

        // actionOf: the live InputAction a row drives (slot 0 primary, 1 secondary).
        // rebuild: rebuilds every action from the table after it changed.
        public BindingRebinder(BindingTable<TAction> table, Func<TAction, InputAction> actionOf, Action rebuild)
        {
            _table    = table;
            _actionOf = actionOf;
            _rebuild  = rebuild;
        }

        public bool   IsListening => _listening;
        public bool   HasMessage  => _messageTimer > 0f;
        // Transient warning shown when a rebind shares a control with another action.
        public string Message     { get; private set; }

        // Bindings or the listening slot changed: the rows need redrawing.
        public event Action Changed;

        public bool IsListeningTo(TAction action, bool primary) =>
            _listening && _primary == primary && EqualityComparer<TAction>.Default.Equals(_action, action);

        public bool IsShared(TAction action, bool primary, string path) =>
            _table.SharedWith(action, primary, path).Length > 0;

        public static string Label(string path, bool omitDevice = false)
        {
            if (string.IsNullOrEmpty(path)) return "—";
            return omitDevice
                ? InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice)
                : InputControlPath.ToHumanReadableString(path);
        }

        public void StartListening(TAction action, bool primary)
        {
            if (!_table.IsRebindable(action)) return;
            Cancel();
            _listening    = true;
            _action       = action;
            _primary      = primary;
            _startFrame   = Time.frameCount;
            _started      = false;
            _messageTimer = 0f;
            Changed?.Invoke();
        }

        public void Tick(float unscaledDeltaTime)
        {
            // A frame late, so the click that chose the slot isn't taken as its new control.
            if (_listening && !_started && Time.frameCount > _startFrame)
            {
                _started = true;
                Begin(_action, _primary);
            }

            TickEscape(unscaledDeltaTime);
            if (_messageTimer > 0f) _messageTimer -= unscaledDeltaTime;
        }

        public void Cancel()
        {
            bool wasListening = _listening;
            bool wasRunning   = _started;
            _operation?.Cancel();
            _operation = null;
            _listening = false;
            _started   = false;
            if (wasRunning) _rebuild();   // re-enables the action and drops the half-done override
            if (wasListening) Changed?.Invoke();
        }

        public void ResetToDefaults()
        {
            Cancel();
            _table.ResetToDefaults();
            _table.DeleteSaved();
            _rebuild();
            Changed?.Invoke();
        }

        private void TickEscape(float unscaledDeltaTime)
        {
            var escape = Keyboard.current?.escapeKey;
            if (!_listening || escape == null)
            {
                _escapeHeld    = 0f;
                _escapeHandled = false;
                return;
            }

            if (escape.isPressed)
            {
                if (_escapeHandled) return;
                _escapeHeld += unscaledDeltaTime;
                if (_escapeHeld < EscapeHoldToClear) return;

                _escapeHandled = true;
                TAction action  = _action;
                bool    primary = _primary;
                Cancel();
                Write(action, primary, string.Empty);
                return;
            }

            if (_escapeHeld > 0f && !_escapeHandled) Cancel();
            _escapeHeld    = 0f;
            _escapeHandled = false;
        }

        private void Begin(TAction action, bool primary)
        {
            InputAction inputAction = _actionOf(action);
            if (inputAction == null)
            {
                Cancel();
                return;
            }

            // Unity won't rebind an enabled action; the rebuild brings it back afterwards.
            inputAction.Disable();
            int index = primary ? 0 : 1;
            _operation = inputAction.PerformInteractiveRebinding(index)
                .WithControlsExcluding("<Keyboard>/escape")
                .WithControlsExcluding("<Keyboard>/anyKey")   // would also match the Escape press used to cancel/clear
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .OnMatchWaitForAnother(0.05f)
                .OnComplete(op => Completed(action, primary, index, op))
                .OnCancel(op => op.Dispose())
                .Start();
        }

        private void Completed(TAction action, bool primary, int index, InputActionRebindingExtensions.RebindingOperation op)
        {
            string path = op.action.bindings[index].effectivePath;
            op.Dispose();
            _operation = null;
            _started   = false;
            _listening = false;

            // Sharing a control between actions is allowed; the warning stays up for a moment
            // and both slots stay highlighted until one of them is changed.
            string shared = _table.SharedWith(action, primary, path);
            Write(action, primary, path);
            if (shared.Length == 0) return;

            Message       = $"{Label(path)} is also bound to {shared}";
            _messageTimer = ConflictMessageSeconds;
        }

        private void Write(TAction action, bool primary, string path)
        {
            int index = _table.IndexOf(action);
            if (index < 0) return;

            _table.SetPath(index, primary, path);
            _rebuild();
            _table.Save();
            Changed?.Invoke();
        }
    }
}
