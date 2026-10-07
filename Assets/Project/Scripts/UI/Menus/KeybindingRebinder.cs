using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CGD.Input;

namespace CGD.UI
{
    // The rebinding flow behind KeybindingSection: which slot is listening, the interactive
    // rebind, Escape to cancel or clear, and shared-control detection. Knows nothing about
    // the UI; it raises events so the view can refresh the rows that changed.
    public class KeybindingRebinder
    {
        private const float EscapeHoldToClear = 0.6f;
        private const float ConflictMessageSeconds = 3f;

        private readonly PlayerInputHandler   _input;
        private readonly InputBindingSettings _bindings;

        // Rebind state: -1 = not listening
        private int  _listeningAction = -1;
        private bool _listeningPrimary;
        private int  _listenStartFrame;
        private bool _rebindStarted;
        private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

        private float _escapeHeldTime;
        private bool  _escapeHoldHandled;

        private float _conflictMessageTimer;

        public bool   IsListening        => _listeningAction >= 0;
        public bool   HasConflictMessage => _conflictMessageTimer > 0f;
        // Transient warning shown when a rebind attempt shares a control with another action.
        public string ConflictMessage    { get; private set; }

        // One action's listening state changed; its row needs a refresh.
        public event Action<GameAction> ActionChanged;
        // Bindings were rewritten (or reset); every row needs a refresh.
        public event Action BindingsChanged;

        public KeybindingRebinder(PlayerInputHandler input, InputBindingSettings bindings)
        {
            _input    = input;
            _bindings = bindings;
        }

        public bool IsListeningTo(GameAction action, bool primary) =>
            _listeningAction == (int)action && _listeningPrimary == primary;

        public static string BindingLabel(string path) =>
            string.IsNullOrEmpty(path) ? "—" : InputControlPath.ToHumanReadableString(path);

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

            if (_conflictMessageTimer > 0f) _conflictMessageTimer -= unscaledDeltaTime;
        }

        public void StartListening(GameAction action, bool primary)
        {
            _listeningAction      = (int)action;
            _listeningPrimary     = primary;
            _listenStartFrame     = Time.frameCount;
            _rebindStarted        = false;
            _conflictMessageTimer = 0f;

            ActionChanged?.Invoke(action);
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

            if (prevAction >= 0) ActionChanged?.Invoke((GameAction)prevAction);
        }

        public void ResetToDefaults()
        {
            CancelRebind();
            _bindings.ResetToDefaults();
            _input.RebuildActions();
            SettingsSave.DeleteBindings();
            BindingsChanged?.Invoke();
        }

        public bool IsShared(GameAction action, bool primary, string path) =>
            FindSharedActions(action, primary, path).Length > 0;

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
            ConflictMessage       = $"{BindingLabel(path)} is also bound to {sharedWith}";
            _conflictMessageTimer = ConflictMessageSeconds;
        }

        // Comma-separated actions that use this control path in a slot other than the one
        // given (so a slot is never shared with itself); empty when the path is unshared.
        private string FindSharedActions(GameAction action, bool primary, string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            var names = new List<string>();
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

            BindingsChanged?.Invoke();
        }
    }
}
