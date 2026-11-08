using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CGD.DevTools
{
    // Rebinding one spectator control at a time, the way the player's keybinding menu does
    // it (KeybindingRebinder): click a slot, press the new control; tap Escape to cancel,
    // hold it to clear the slot. A control already used by another spectator action is
    // allowed but called out. Knows nothing about the UI.
    internal sealed class SpectatorRebinder
    {
        private const float EscapeHoldToClear      = 0.6f;
        private const float ConflictMessageSeconds = 3f;

        private readonly SpectatorInput         _input;
        private readonly SpectatorInputSettings _settings;

        private int  _listening = -1;   // the action being rebound; −1 = none
        private bool _primary;
        private int  _startFrame;
        private bool _started;
        private InputActionRebindingExtensions.RebindingOperation _operation;
        private float _escapeHeld;
        private bool  _escapeHandled;
        private float _messageTimer;

        public SpectatorRebinder(SpectatorInput input, SpectatorInputSettings settings)
        {
            _input    = input;
            _settings = settings;
        }

        public bool   IsListening => _listening >= 0;
        public bool   HasMessage  => _messageTimer > 0f;
        public string Message     { get; private set; }

        // Bindings changed (rebound, cleared or reset): the list needs redrawing.
        public event Action Changed;

        public bool IsListeningTo(SpectatorAction action, bool primary) => _listening == (int)action && _primary == primary;

        public static string Label(string path) =>
            string.IsNullOrEmpty(path) ? "—" : InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);

        public void StartListening(SpectatorAction action, bool primary)
        {
            if (SpectatorInputSettings.IsFixed(action)) return;
            Cancel();
            _listening    = (int)action;
            _primary      = primary;
            _startFrame   = Time.frameCount;
            _started      = false;
            _messageTimer = 0f;
            Changed?.Invoke();
        }

        public void Tick(float unscaledDeltaTime)
        {
            // A frame late, so the click that chose the slot isn't taken as its new control.
            if (IsListening && !_started && Time.frameCount > _startFrame)
            {
                _started = true;
                Begin((SpectatorAction)_listening, _primary);
            }

            TickEscape(unscaledDeltaTime);
            if (_messageTimer > 0f) _messageTimer -= unscaledDeltaTime;
        }

        public void Cancel()
        {
            bool wasRunning = _started;
            _operation?.Cancel();
            _operation = null;
            _listening = -1;
            _started   = false;
            if (wasRunning) _input.Rebuild();   // re-enables the action and drops the half-done override
            Changed?.Invoke();
        }

        public void ResetToDefaults()
        {
            Cancel();
            _settings.ResetBindings();
            SpectatorSave.DeleteBindings();
            _input.Rebuild();
            Changed?.Invoke();
        }

        // Another spectator action that uses this control (empty if none).
        public string SharedWith(SpectatorAction action, bool primary, string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            var names = new List<string>();
            foreach (SpectatorBinding binding in _settings.Bindings)
            {
                bool samePrimary   = binding.Action == action && primary;
                bool sameSecondary = binding.Action == action && !primary;
                if ((!samePrimary && binding.PrimaryPath == path) || (!sameSecondary && binding.SecondaryPath == path))
                    names.Add(SpectatorActionLabels.Of(binding.Action));
            }
            return string.Join(", ", names);
        }

        private void TickEscape(float unscaledDeltaTime)
        {
            var escape = Keyboard.current?.escapeKey;
            if (!IsListening || escape == null)
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
                var action  = (SpectatorAction)_listening;
                bool primary = _primary;
                Cancel();
                Write(action, primary, string.Empty);
                return;
            }

            if (_escapeHeld > 0f && !_escapeHandled) Cancel();
            _escapeHeld    = 0f;
            _escapeHandled = false;
        }

        private void Begin(SpectatorAction action, bool primary)
        {
            InputAction inputAction = _input.Get(action);
            if (inputAction == null)
            {
                Cancel();
                return;
            }

            // Unity won't rebind an enabled action; Rebuild() brings it back afterwards.
            inputAction.Disable();
            int index = primary ? 0 : 1;
            _operation = inputAction.PerformInteractiveRebinding(index)
                .WithControlsExcluding("<Keyboard>/escape")
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .OnMatchWaitForAnother(0.05f)
                .OnComplete(op => Completed(action, primary, index, op))
                .OnCancel(op => op.Dispose())
                .Start();
        }

        private void Completed(SpectatorAction action, bool primary, int index, InputActionRebindingExtensions.RebindingOperation op)
        {
            string path = op.action.bindings[index].effectivePath;
            op.Dispose();
            _operation = null;
            _started   = false;
            _listening = -1;

            string shared = SharedWith(action, primary, path);
            Write(action, primary, path);
            if (shared.Length == 0) return;

            Message       = $"{Label(path)} is also used for {shared}";
            _messageTimer = ConflictMessageSeconds;
        }

        private void Write(SpectatorAction action, bool primary, string path)
        {
            int i = _settings.IndexOf(action);
            if (i < 0) return;

            SpectatorBinding binding = _settings.Bindings[i];
            if (primary) binding.PrimaryPath = path;
            else         binding.SecondaryPath = path;
            _settings.Bindings[i] = binding;

            _input.Rebuild();
            SpectatorSave.SaveBindings(_settings);
            Changed?.Invoke();
        }
    }
}
