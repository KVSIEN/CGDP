using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CGD.DevTools
{
    // Reads the spectator's controls: one InputAction per SpectatorAction, built at runtime
    // from SpectatorInputSettings (as PlayerInputHandler does for the player), plus the
    // analog parts that always work — mouse movement and pointer position, both sticks and
    // the triggers for up / down. Only enabled while spectating.
    internal sealed class SpectatorInput : IDisposable
    {
        private static readonly int ActionCount = Enum.GetValues(typeof(SpectatorAction)).Length;

        private readonly SpectatorInputSettings _settings;
        private readonly InputAction[] _actions = new InputAction[ActionCount];
        private readonly InputAction   _pointerDelta;
        private readonly InputAction   _pointerPosition;
        private readonly InputAction   _stickMove;
        private readonly InputAction   _stickLook;
        private readonly InputAction   _triggerUp;
        private readonly InputAction   _triggerDown;
        private bool _enabled;

        public SpectatorInput(SpectatorInputSettings settings)
        {
            _settings = settings;
            _settings.EnsureComplete();
            SpectatorSave.LoadBindings(_settings);

            _pointerDelta    = Value("Pointer Delta",    "<Pointer>/delta",    "Vector2");
            _pointerPosition = Value("Pointer Position", "<Pointer>/position", "Vector2");
            _stickMove       = Value("Stick Move",       "<Gamepad>/leftStick",  "Vector2");
            _stickLook       = Value("Stick Look",       "<Gamepad>/rightStick", "Vector2");
            _triggerUp       = Value("Trigger Up",       "<Gamepad>/rightTrigger", "Axis");
            _triggerDown     = Value("Trigger Down",     "<Gamepad>/leftTrigger",  "Axis");
            Build();
            _settings.NotifyChanged();
        }

        // x = right, y = up, z = forward, each −1…1: keys, the left stick and the triggers.
        public Vector3 Move
        {
            get
            {
                Vector2 stick = _stickMove.ReadValue<Vector2>();
                float x = Axis(SpectatorAction.MoveRight, SpectatorAction.MoveLeft) + stick.x;
                float y = Axis(SpectatorAction.MoveUp, SpectatorAction.MoveDown) +
                          _triggerUp.ReadValue<float>() - _triggerDown.ReadValue<float>();
                float z = Axis(SpectatorAction.MoveForward, SpectatorAction.MoveBack) + stick.y;
                return new Vector3(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f), Mathf.Clamp(z, -1f, 1f));
            }
        }

        // Pixels the mouse moved this frame.
        public Vector2 PointerDelta    => _pointerDelta.ReadValue<Vector2>();
        public Vector2 PointerPosition => _pointerPosition.ReadValue<Vector2>();
        // Right stick, −1…1 (turns the camera without holding Look).
        public Vector2 StickLook       => _stickLook.ReadValue<Vector2>();
        // Zoom steps this frame: + in, − out.
        public int     Zoom            => (WasPressed(SpectatorAction.ZoomIn) ? 1 : 0) - (WasPressed(SpectatorAction.ZoomOut) ? 1 : 0);

        public bool IsHeld(SpectatorAction action)     => _enabled && (_actions[(int)action]?.IsPressed() ?? false);
        public bool WasPressed(SpectatorAction action) => _enabled && (_actions[(int)action]?.WasPressedThisFrame() ?? false);

        // The action itself, for interactive rebinding (SpectatorRebinder).
        public InputAction Get(SpectatorAction action) => _actions[(int)action];

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            foreach (InputAction action in AllActions())
            {
                if (action == null) continue;
                if (enabled) action.Enable();
                else action.Disable();
            }
        }

        // After a rebind or reset: actions come back with the new paths.
        public void Rebuild()
        {
            for (int i = 0; i < _actions.Length; i++)
            {
                _actions[i]?.Dispose();
                _actions[i] = null;
            }
            Build();
            _settings.NotifyChanged();
        }

        public void Dispose()
        {
            foreach (InputAction action in AllActions()) action?.Dispose();
        }

        private void Build()
        {
            foreach (SpectatorBinding binding in _settings.Bindings)
            {
                var action = new InputAction(binding.Action.ToString(), InputActionType.Button);
                // Index 0 is always the primary slot and 1 the secondary, set or not, so
                // rebinding has a stable index to target.
                action.AddBinding(string.IsNullOrEmpty(binding.PrimaryPath) ? null : binding.PrimaryPath);
                action.AddBinding(string.IsNullOrEmpty(binding.SecondaryPath) ? null : binding.SecondaryPath);
                if (_enabled) action.Enable();
                _actions[(int)binding.Action] = action;
            }
        }

        private float Axis(SpectatorAction positive, SpectatorAction negative) =>
            (IsHeld(positive) ? 1f : 0f) - (IsHeld(negative) ? 1f : 0f);

        private System.Collections.Generic.IEnumerable<InputAction> AllActions()
        {
            foreach (InputAction action in _actions) yield return action;
            yield return _pointerDelta;
            yield return _pointerPosition;
            yield return _stickMove;
            yield return _stickLook;
            yield return _triggerUp;
            yield return _triggerDown;
        }

        private static InputAction Value(string name, string path, string controlType)
        {
            var action = new InputAction(name, InputActionType.Value, expectedControlType: controlType);
            action.AddBinding(path);
            return action;
        }
    }
}
