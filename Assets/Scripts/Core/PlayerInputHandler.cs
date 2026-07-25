using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private InputBindingSettings _bindings;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsGamepadLook { get; private set; }

    private static readonly int ActionCount = Enum.GetValues(typeof(GameAction)).Length;

    private const float DoubleClickWindow = 0.3f;

    // Move/Look are built here alongside every other GameAction instead of coming from a
    // separate .inputactions asset, so all player-facing input lives in one place.
    private InputAction _moveAction;
    private InputAction _lookAction;

    private InputAction[] _actions;
    private InputActionMode[] _modes;
    private bool[] _results;
    private bool[] _toggleStates;
    private float[] _lastClickTime;
    private bool[] _waitingForDoubleClick;

    public bool InputEnabled { get; set; } = true;

    private void Awake()
    {
        _actions = new InputAction[ActionCount];
        _modes = new InputActionMode[ActionCount];
        _results = new bool[ActionCount];
        _toggleStates = new bool[ActionCount];
        _lastClickTime = new float[ActionCount];
        _waitingForDoubleClick = new bool[ActionCount];

        BuildAxisActions();
        SettingsSave.LoadBindings(_bindings);
        BuildActions();
    }

    private void BuildAxisActions()
    {
        _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");
        _moveAction.AddBinding("<Gamepad>/leftStick");
        _moveAction.Enable();

        _lookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
        _lookAction.AddBinding("<Pointer>/delta");
        _lookAction.AddBinding("<Gamepad>/rightStick");
        _lookAction.Enable();
    }

    private void BuildActions()
    {
        foreach (var binding in _bindings.Bindings)
        {
            int i = (int)binding.Action;
            var action = new InputAction(binding.Action.ToString(), InputActionType.Button);

            // Binding index 0 is always the primary slot, 1 always the secondary — added
            // even when empty (as an unbound placeholder) so PerformInteractiveRebinding
            // has a stable index to target regardless of whether the slot is currently set.
            AddSlot(action, binding.PrimaryPath);
            AddSlot(action, binding.SecondaryPath);

            action.Enable();
            _actions[i] = action;
            _modes[i] = binding.Mode;
        }
    }

    private static void AddSlot(InputAction action, string path)
    {
        action.AddBinding(string.IsNullOrEmpty(path) ? null : path);
    }

    private void OnEnable()
    {
        _moveAction?.Enable();
        _lookAction?.Enable();
    }

    private void OnDisable()
    {
        _moveAction?.Disable();
        _lookAction?.Disable();
        foreach (var a in _actions) a?.Disable();
    }

    private void OnDestroy()
    {
        _moveAction?.Dispose();
        _lookAction?.Dispose();
        foreach (var a in _actions) a?.Dispose();
    }

    private void Update()
    {
        if (!InputEnabled)
        {
            MoveInput     = Vector2.zero;
            LookInput     = Vector2.zero;
            IsGamepadLook = false;
            Array.Clear(_results, 0, _results.Length);
            return;
        }

        MoveInput = _moveAction.ReadValue<Vector2>();
        LookInput = _lookAction.ReadValue<Vector2>();
        IsGamepadLook = _lookAction.activeControl?.device is Gamepad;

        float now = Time.unscaledTime;
        for (int i = 0; i < ActionCount; i++)
        {
            if (_waitingForDoubleClick[i] && now - _lastClickTime[i] > DoubleClickWindow)
                _waitingForDoubleClick[i] = false;
        }

        for (int i = 0; i < ActionCount; i++)
        {
            var action = _actions[i];
            if (action == null) continue;

            _results[i] = _modes[i] switch
            {
                InputActionMode.Pressed      => action.WasPressedThisFrame(),
                InputActionMode.Held         => action.IsPressed(),
                InputActionMode.Toggle       => EvaluateToggle(i, action),
                InputActionMode.DoubleClick  => EvaluateDoubleClick(i, action),
                _                            => false
            };
        }
    }

    private bool EvaluateToggle(int i, InputAction action)
    {
        if (action.WasPressedThisFrame())
            _toggleStates[i] = !_toggleStates[i];
        return _toggleStates[i];
    }

    private bool EvaluateDoubleClick(int i, InputAction action)
    {
        if (!action.WasPressedThisFrame()) return false;

        float now = Time.unscaledTime;
        if (_waitingForDoubleClick[i] && now - _lastClickTime[i] <= DoubleClickWindow)
        {
            _waitingForDoubleClick[i] = false;
            return true;
        }

        _lastClickTime[i] = now;
        _waitingForDoubleClick[i] = true;
        return false;
    }

    // Mode-aware result — use this for most gameplay checks
    public bool GetAction(GameAction action) => _results[(int)action];

    // Raw held state regardless of mode — use for sustained checks (e.g. low-jump gravity)
    public bool IsHeld(GameAction action) => InputEnabled && (_actions[(int)action]?.IsPressed() ?? false);

    // Raw press this frame regardless of mode
    public bool WasPressed(GameAction action) => InputEnabled && (_actions[(int)action]?.WasPressedThisFrame() ?? false);

    // Bypasses InputEnabled — use only for overlay toggles (inventory, pause) that must work while input is locked
    public bool WasPressedRaw(GameAction action) => _actions[(int)action]?.WasPressedThisFrame() ?? false;

    // Exposes the underlying InputAction for the settings menu to drive
    // InputActionRebindingExtensions.PerformInteractiveRebinding directly.
    public InputAction GetInputAction(GameAction action) => _actions[(int)action];

    public void SetMode(GameAction action, InputActionMode mode)
    {
        int i = (int)action;
        _modes[i] = mode;
        _toggleStates[i] = false;
    }

    public void RebuildActions()
    {
        for (int i = 0; i < _actions.Length; i++)
        {
            _actions[i]?.Disable();
            _actions[i]?.Dispose();
            _actions[i] = null;
        }
        BuildActions();
    }
}
