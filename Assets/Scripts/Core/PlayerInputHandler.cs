using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private InputBindingSettings _bindings;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    private static readonly int ActionCount = Enum.GetValues(typeof(GameAction)).Length;

    private InputSystem_Actions _axisActions;
    private InputAction[] _actions;
    private InputActionMode[] _modes;
    private bool[] _results;
    private bool[] _toggleStates;

    private void Awake()
    {
        _axisActions = new InputSystem_Actions();
        _actions = new InputAction[ActionCount];
        _modes = new InputActionMode[ActionCount];
        _results = new bool[ActionCount];
        _toggleStates = new bool[ActionCount];

        BuildActions();
    }

    private void BuildActions()
    {
        foreach (var binding in _bindings.Bindings)
        {
            int i = (int)binding.Action;
            var action = new InputAction(binding.Action.ToString(), InputActionType.Button);

            if (binding.PrimaryKey != Key.None)
                action.AddBinding(KeyPath(binding.PrimaryKey));
            if (binding.PrimaryMouse != InputMouseButton.None)
                action.AddBinding(MousePath(binding.PrimaryMouse));
            if (binding.SecondaryKey != Key.None)
                action.AddBinding(KeyPath(binding.SecondaryKey));
            if (binding.SecondaryMouse != InputMouseButton.None)
                action.AddBinding(MousePath(binding.SecondaryMouse));

            action.Enable();
            _actions[i] = action;
            _modes[i] = binding.Mode;
        }
    }

    private void OnEnable() => _axisActions.Enable();

    private void OnDisable()
    {
        _axisActions.Disable();
        foreach (var a in _actions) a?.Disable();
    }

    private void OnDestroy()
    {
        foreach (var a in _actions) a?.Dispose();
    }

    private void Update()
    {
        MoveInput = _axisActions.Player.Move.ReadValue<Vector2>();
        LookInput = _axisActions.Player.Look.ReadValue<Vector2>();

        for (int i = 0; i < ActionCount; i++)
        {
            var action = _actions[i];
            if (action == null) continue;

            _results[i] = _modes[i] switch
            {
                InputActionMode.Pressed => action.WasPressedThisFrame(),
                InputActionMode.Held    => action.IsPressed(),
                InputActionMode.Toggle  => EvaluateToggle(i, action),
                _                       => false
            };
        }
    }

    private bool EvaluateToggle(int i, InputAction action)
    {
        if (action.WasPressedThisFrame())
            _toggleStates[i] = !_toggleStates[i];
        return _toggleStates[i];
    }

    // Mode-aware result — use this for most gameplay checks
    public bool GetAction(GameAction action) => _results[(int)action];

    // Raw held state regardless of mode — use for sustained checks (e.g. low-jump gravity)
    public bool IsHeld(GameAction action) => _actions[(int)action]?.IsPressed() ?? false;

    // Raw press this frame regardless of mode
    public bool WasPressed(GameAction action) => _actions[(int)action]?.WasPressedThisFrame() ?? false;

    public void SetMode(GameAction action, InputActionMode mode)
    {
        int i = (int)action;
        _modes[i] = mode;
        _toggleStates[i] = false;
    }

    public void Remap(GameAction action, Key primary, Key secondary = Key.None,
        InputMouseButton primaryMouse = InputMouseButton.None,
        InputMouseButton secondaryMouse = InputMouseButton.None)
    {
        int i = (int)action;
        _actions[i]?.Disable();
        _actions[i]?.Dispose();

        var a = new InputAction(action.ToString(), InputActionType.Button);
        if (primary != Key.None) a.AddBinding(KeyPath(primary));
        if (primaryMouse != InputMouseButton.None) a.AddBinding(MousePath(primaryMouse));
        if (secondary != Key.None) a.AddBinding(KeyPath(secondary));
        if (secondaryMouse != InputMouseButton.None) a.AddBinding(MousePath(secondaryMouse));

        a.Enable();
        _actions[i] = a;
    }

    // Unity's input system paths use camelCase, e.g. "<Keyboard>/leftShift".
    // Digit keys are an exception: the Key enum says "Digit3" but the control
    // path is just the digit — "<Keyboard>/3", not "<Keyboard>/digit3".
    private static string KeyPath(Key key)
    {
        string name = key.ToString();
        if (name.StartsWith("Digit"))
            return $"<Keyboard>/{name[5..]}";
        return $"<Keyboard>/{char.ToLower(name[0])}{name[1..]}";
    }

    private static string MousePath(InputMouseButton btn) => btn switch
    {
        InputMouseButton.Left    => "<Mouse>/leftButton",
        InputMouseButton.Right   => "<Mouse>/rightButton",
        InputMouseButton.Middle  => "<Mouse>/middleButton",
        InputMouseButton.Forward => "<Mouse>/forwardButton",
        InputMouseButton.Back    => "<Mouse>/backButton",
        _                        => string.Empty
    };
}
