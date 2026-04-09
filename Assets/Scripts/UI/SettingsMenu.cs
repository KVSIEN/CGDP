using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ESC toggles this menu open/closed.
/// Add this component to any GameObject in the scene and wire up the serialized references.
/// No Canvas or prefab setup required — the menu draws itself via OnGUI.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private PlayerCamera        _camera;
    [SerializeField] private PlayerInputHandler  _input;
    [SerializeField] private InputBindingSettings _bindings;
    [SerializeField] private HUDManager          _hud;

    private bool    _isOpen;
    private Vector2 _scrollPos;

    private float  _pendingMouse;
    private float  _pendingGamepad;
    private string _mouseSensText;
    private string _gamepadSensText;

    // Rebind state: -1 = not listening
    private int  _listeningAction = -1;
    private bool _listeningPrimary;
    private int  _listenStartFrame;

    private static readonly string[] ModeLabels = { "Press", "Hold", "Toggle", "Dbl Click" };

    private const float W = 680f;
    private const float H = 520f;

    private void Start()
    {
        SettingsSave.LoadSensitivity(out _pendingMouse, out _pendingGamepad);
        _mouseSensText   = _pendingMouse.ToString("F2");
        _gamepadSensText = _pendingGamepad.ToString("F0");
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_listeningAction >= 0) { _listeningAction = -1; return; }
            if (_isOpen) Close(); else Open();
            return;
        }

        if (_listeningAction >= 0 && Time.frameCount > _listenStartFrame)
            PollForRebind();
    }

    private void PollForRebind()
    {
        var mouse = Mouse.current;
        InputMouseButton mb = InputMouseButton.None;
        if (mouse.leftButton.wasPressedThisFrame)        mb = InputMouseButton.Left;
        else if (mouse.rightButton.wasPressedThisFrame)  mb = InputMouseButton.Right;
        else if (mouse.middleButton.wasPressedThisFrame) mb = InputMouseButton.Middle;
        else if (mouse.forwardButton.wasPressedThisFrame) mb = InputMouseButton.Forward;
        else if (mouse.backButton.wasPressedThisFrame)   mb = InputMouseButton.Back;

        if (mb != InputMouseButton.None)
        {
            WriteRebind(Key.None, mb);
            return;
        }

        var kb = Keyboard.current;
        foreach (Key key in Enum.GetValues(typeof(Key)))
        {
            if (key == Key.None || key == Key.Escape) continue;
            var ctrl = kb[key];
            if (ctrl != null && ctrl.wasPressedThisFrame)
            {
                WriteRebind(key, InputMouseButton.None);
                return;
            }
        }
    }

    private void WriteRebind(Key key, InputMouseButton mb)
    {
        var action = (GameAction)_listeningAction;
        for (int i = 0; i < _bindings.Bindings.Count; i++)
        {
            if (_bindings.Bindings[i].Action != action) continue;
            var b = _bindings.Bindings[i];
            if (_listeningPrimary)
            {
                b.PrimaryKey   = key;
                b.PrimaryMouse = mb;
            }
            else
            {
                b.SecondaryKey   = key;
                b.SecondaryMouse = mb;
            }
            _bindings.Bindings[i] = b;
            break;
        }

        _input.RebuildActions();
        SettingsSave.SaveBindings(_bindings);
        _listeningAction = -1;
    }

    private void Open()
    {
        _isOpen = true;
        _input.InputEnabled = false;
        _hud.HideAll();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        SettingsSave.LoadSensitivity(out _pendingMouse, out _pendingGamepad);
        _mouseSensText   = _pendingMouse.ToString("F2");
        _gamepadSensText = _pendingGamepad.ToString("F0");
    }

    private void Close()
    {
        _listeningAction    = -1;
        _isOpen             = false;
        _input.InputEnabled = true;
        _hud.ShowAll();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void OnGUI()
    {
        if (!_isOpen) return;

        float x = (Screen.width  - W) * 0.5f;
        float y = (Screen.height - H) * 0.5f;
        GUI.Window(0, new Rect(x, y, W, H), DrawWindow, "Settings");

        if (_listeningAction >= 0)
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            GUI.Label(new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f - 25f, 400f, 50f),
                "Press any key or mouse button\n(Escape to cancel)", style);
        }
    }

    private void DrawWindow(int id)
    {
        float y = 20f;

        // ── Sensitivity ──────────────────────────────────────────────────
        GUI.Label(new Rect(10, y, 580, 20), "── Sensitivity ──");
        y += 24f;

        GUI.Label(new Rect(10, y, 60, 20), "Mouse:");
        float prevMouse = _pendingMouse;
        _pendingMouse = GUI.HorizontalSlider(new Rect(75, y + 4f, 330f, 16f), _pendingMouse, 0.1f, 10f);
        if (_pendingMouse != prevMouse) _mouseSensText = _pendingMouse.ToString("F2");
        string newMouseText = GUI.TextField(new Rect(415, y, 70, 20), _mouseSensText);
        if (newMouseText != _mouseSensText)
        {
            _mouseSensText = newMouseText;
            if (float.TryParse(newMouseText, out float parsedMouse))
                _pendingMouse = Mathf.Clamp(parsedMouse, 0.1f, 10f);
        }
        y += 26f;

        GUI.Label(new Rect(10, y, 60, 20), "Gamepad:");
        float prevGamepad = _pendingGamepad;
        _pendingGamepad = GUI.HorizontalSlider(new Rect(75, y + 4f, 330f, 16f), _pendingGamepad, 10f, 300f);
        if (_pendingGamepad != prevGamepad) _gamepadSensText = _pendingGamepad.ToString("F0");
        string newGamepadText = GUI.TextField(new Rect(415, y, 70, 20), _gamepadSensText);
        if (newGamepadText != _gamepadSensText)
        {
            _gamepadSensText = newGamepadText;
            if (float.TryParse(newGamepadText, out float parsedGamepad))
                _pendingGamepad = Mathf.Clamp(parsedGamepad, 10f, 300f);
        }
        y += 28f;

        if (GUI.Button(new Rect(170, y, 140, 22), "Apply Sensitivity"))
        {
            _camera.SetSensitivity(_pendingMouse, _pendingGamepad);
            SettingsSave.SaveSensitivity(_pendingMouse, _pendingGamepad);
        }
        y += 34f;

        // ── Keybindings ───────────────────────────────────────────────────
        GUI.Label(new Rect(10, y, 580, 20), "── Keybindings ──");
        y += 22f;

        GUI.Label(new Rect(10,  y, 135, 20), "Action");
        GUI.Label(new Rect(150, y, 120, 20), "Primary");
        GUI.Label(new Rect(275, y, 120, 20), "Secondary");
        GUI.Label(new Rect(400, y, 200, 20), "Mode");
        y += 20f;

        float scrollHeight  = H - y - 36f;
        float contentHeight = _bindings.Bindings.Count * 28f;
        _scrollPos = GUI.BeginScrollView(
            new Rect(10, y, W - 20f, scrollHeight),
            _scrollPos,
            new Rect(0, 0, W - 40f, contentHeight));

        float ry = 0f;
        for (int i = 0; i < _bindings.Bindings.Count; i++)
        {
            var b = _bindings.Bindings[i];
            bool waitPri = _listeningAction == (int)b.Action && _listeningPrimary;
            bool waitSec = _listeningAction == (int)b.Action && !_listeningPrimary;

            GUI.Label(new Rect(0, ry, 140, 24), b.Action.ToString());

            if (GUI.Button(new Rect(145, ry, 120, 24), waitPri ? "▪▪▪" : BindingLabel(b.PrimaryKey, b.PrimaryMouse)))
            {
                _listeningAction  = (int)b.Action;
                _listeningPrimary = true;
                _listenStartFrame = Time.frameCount;
            }
            if (GUI.Button(new Rect(270, ry, 120, 24), waitSec ? "▪▪▪" : BindingLabel(b.SecondaryKey, b.SecondaryMouse)))
            {
                _listeningAction  = (int)b.Action;
                _listeningPrimary = false;
                _listenStartFrame = Time.frameCount;
            }

            int newMode = GUI.SelectionGrid(new Rect(395, ry, 195, 24), (int)b.Mode, ModeLabels, 3);
            if (newMode != (int)b.Mode)
            {
                b.Mode = (InputActionMode)newMode;
                _bindings.Bindings[i] = b;
                _input.SetMode(b.Action, b.Mode);
                SettingsSave.SaveBindings(_bindings);
            }

            ry += 28f;
        }
        GUI.EndScrollView();

        // ── Bottom buttons ────────────────────────────────────────────────
        float by = H - 30f;
        if (GUI.Button(new Rect(10, by, 130, 24), "Reset Defaults"))
        {
            _bindings.ResetToDefaults();
            _input.RebuildActions();
            SettingsSave.DeleteBindings();
        }
        if (GUI.Button(new Rect(W - 90f, by, 78f, 24), "Close"))
            Close();
    }

    private static string BindingLabel(Key key, InputMouseButton mouse)
    {
        if (mouse != InputMouseButton.None) return mouse.ToString();
        if (key   != Key.None)             return key.ToString();
        return "—";
    }
}
