using UnityEngine;
using UnityEngine.EventSystems;
using CGD.Core;
using CGD.Enemies;

namespace CGD.DevTools
{
    // Spectator mode for watching AI with the player switched off. It takes over the moment
    // the gameplay camera is off (deactivate the Player) and steps aside when it comes back.
    // While on it runs its own camera (tagged MainCamera, so world-space bars face it) and
    // audio listener, its own controls (SpectatorInputSettings, rebindable in its settings
    // panel), its own HUD, labels over every agent under _agents, and pause / slow motion.
    //
    //   watch     1–4, Tab / Shift+Tab, or click an agent: the camera orbits it and the
    //             inspector shows it; hold the right mouse to orbit, scroll to get closer
    //   arena     0: back to the placed overview; WASD pans it, scroll zooms
    //   free      F, or just steer away (move while watching, look while in the overview)
    //   inspect   the watched agent stays inspected when the camera leaves it; I pages
    //             through its overview, blackboard and decisions; click empty ground to stop
    //   overlays  V senses, C cover, N paths & plans, drawn in the Game view
    //
    // The camera works on real time, so it moves freely while the game is paused. This
    // component wires Unity to the plain classes that do the work: SpectatorInput,
    // SpectatorCameraController, SpectatorTime, SpectatorHud, SpectatorSettingsPanel,
    // AgentLabels, AgentInspector and SpectatorOverlays.
    public class SpectatorCamera : MonoBehaviour
    {
        private const float FocusHeight = 1.2f;   // metres above an agent's feet the camera orbits
        private const float PickRadius  = 70f;    // pixels at 1080p around an agent or its label that count as clicking it
        private const float HudRefresh  = 0.25f;
        private const int   WatchKeys   = 4;

        [SerializeField] private Camera        _camera;
        [SerializeField] private AudioListener _listener;
        [Tooltip("The Player's camera: spectator mode runs only while this one is off")]
        [SerializeField] private Camera        _gameplayCamera;
        [Tooltip("Every EnemyAI under this can be watched and gets a label")]
        [SerializeField] private Transform     _agents;
        [Tooltip("The spectator's controls and default feel (Data/DevTools/SpectatorInputSettings)")]
        [SerializeField] private SpectatorInputSettings _inputSettings;
        [Tooltip("Metres the overview may pan away from where it looks at first")]
        [SerializeField, Min(0f)] private float _panLimit = 30f;
        [Tooltip("Transparent material with vertex colours for the world overlays (Sprites-Default). Empty = made from the Sprites/Default shader")]
        [SerializeField] private Material _overlayMaterial;

        [Header("Labels")]
        [Tooltip("Metres above an agent's feet the label sits at")]
        [SerializeField, Min(0f)] private float   _labelHeight = 3f;
        [Tooltip("Label box, in pixels at 1920 × 1080")]
        [SerializeField] private Vector2 _labelSize = new(250f, 66f);
        [SerializeField, Min(6f)] private float   _fontSize = 15f;

        private readonly SpectatorTime  _time      = new();
        private readonly AgentInspector _inspector = new();

        private EnemyAI[]                 _watchable;
        private SpectatorInput            _input;
        private SpectatorPreferences      _preferences;
        private SpectatorCameraController _controller;
        private SpectatorHud              _hud;
        private SpectatorSettingsPanel    _settingsPanel;
        private AgentLabels               _labels;
        private SpectatorOverlays         _overlays;

        private int    _followed  = -1;
        private int    _inspected = -1;
        private bool   _spectating;
        private bool   _cursorCaptured;
        private float  _nextHud;
        private string _helpText;
        private string _hintText;
        private string _overlayText;

        private void Awake()
        {
            // Works without an asset too, on the built-in defaults.
            if (_inputSettings == null)
            {
                _inputSettings = ScriptableObject.CreateInstance<SpectatorInputSettings>();
                _inputSettings.ResetBindings();
            }

            _watchable     = _agents != null ? _agents.GetComponentsInChildren<EnemyAI>(true) : System.Array.Empty<EnemyAI>();
            _input         = new SpectatorInput(_inputSettings);
            _preferences   = SpectatorSave.LoadPreferences(_inputSettings);
            _controller    = new SpectatorCameraController(transform.position, transform.rotation, _panLimit);
            _labels        = new AgentLabels(transform, _watchable, _labelSize, _fontSize);
            _hud           = new SpectatorHud(transform);
            _settingsPanel = new SpectatorSettingsPanel(_hud.Root, _inputSettings, _input, _preferences, ApplyPreferences);
            _overlays      = new SpectatorOverlays(OverlayMaterial(), _watchable);

            _inputSettings.Changed += OnBindingsChanged;
            OnBindingsChanged();
            ApplyPreferences();
            Apply(ShouldSpectate);
        }

        private void OnDisable()
        {
            if (_spectating) Apply(false);
        }

        private void OnDestroy()
        {
            if (_inputSettings != null) _inputSettings.Changed -= OnBindingsChanged;
            _input?.Dispose();
            _overlays?.Dispose();
            _inspector.Clear();
            _time.Release();
        }

        // Late, so the camera and labels follow agents after they moved this frame.
        private void LateUpdate()
        {
            bool spectate = ShouldSpectate;
            if (spectate != _spectating) Apply(spectate);
            if (!_spectating) return;

            float now = Time.unscaledTime;
            float dt  = Time.unscaledDeltaTime;
            _time.Tick();
            _settingsPanel.Tick(dt);
            HandleCommands();
            Steer(dt);

            _labels.Highlight(_inspected);
            _labels.Tick(_camera, _labelHeight, now);
            _overlays.Tick(_camera, _preferences, _inspector.Agent, now);
            _hud.Tick(now);
            if (now >= _nextHud) RefreshHud(now);
        }

        private bool ShouldSpectate => _gameplayCamera == null || !_gameplayCamera.isActiveAndEnabled;

        private void Apply(bool spectate)
        {
            _spectating = spectate;
            if (_camera != null) _camera.enabled = spectate;
            if (_listener != null) _listener.enabled = spectate;
            _input.SetEnabled(spectate);
            _hud.SetVisible(spectate);
            _labels.SetVisible(spectate && _preferences.ShowLabels);
            _nextHud = 0f;

            if (spectate)
            {
                CursorLock.Set(false);
                return;
            }
            _settingsPanel.Close();
            _time.Release();
            _inspector.Clear();   // stops recording its trace
            _inspected = -1;
            CaptureCursor(false);
        }

        // --- Commands -------------------------------------------------------------------

        private void HandleCommands()
        {
            // Escape belongs to the rebind prompt while one is listening.
            if (_settingsPanel.IsRebinding) return;
            if (Pressed(SpectatorAction.Settings))
            {
                _settingsPanel.Toggle();
                CaptureCursor(false);
                _nextHud = 0f;
                return;
            }
            if (_settingsPanel.IsOpen) return;

            if (Pressed(SpectatorAction.Overview))   ShowOverview();
            if (Pressed(SpectatorAction.FreeCamera)) FlyFree("Free camera");
            if (Pressed(SpectatorAction.NextAgent))     Cycle(_input.IsHeld(SpectatorAction.Fast) ? -1 : 1);
            if (Pressed(SpectatorAction.PreviousAgent)) Cycle(-1);
            for (int i = 0; i < WatchKeys; i++)
                if (Pressed(SpectatorAction.WatchAgent1 + i)) Watch(i);
            if (Pressed(SpectatorAction.Select)) TryPick();

            if (Pressed(SpectatorAction.Pause))
            {
                _time.TogglePause();
                Toast(_time.IsPaused ? $"Paused  ·  {Prompt(SpectatorAction.StepFrame)} steps one frame" : "Resumed");
            }
            if (Pressed(SpectatorAction.SlowDown)) { _time.Slower(); Toast($"Game speed {SpeedText()}"); }
            if (Pressed(SpectatorAction.SpeedUp))  { _time.Faster(); Toast($"Game speed {SpeedText()}"); }
            if (Pressed(SpectatorAction.StepFrame))
            {
                if (_time.IsPaused) _time.StepFrame();
                else Toast($"Pause first ({Prompt(SpectatorAction.Pause)}) to step frames");
            }
            if (Pressed(SpectatorAction.ToggleLabels))
            {
                _preferences.ShowLabels = !_preferences.ShowLabels;
                _labels.SetVisible(_preferences.ShowLabels);
                SpectatorSave.SavePreferences(_preferences);
                Toast(_preferences.ShowLabels ? "Labels on" : "Labels off");
            }
            if (Pressed(SpectatorAction.ToggleHelp))
            {
                _preferences.ShowHelp = !_preferences.ShowHelp;
                SpectatorSave.SavePreferences(_preferences);
                _nextHud = 0f;
            }
            if (Pressed(SpectatorAction.InspectorPage)) NextInspectorPage();
            if (Pressed(SpectatorAction.ToggleSenses)) ToggleOverlay(ref _preferences.ShowSenses, "Senses");
            if (Pressed(SpectatorAction.ToggleCover))  ToggleOverlay(ref _preferences.ShowCover, "Cover");
            if (Pressed(SpectatorAction.TogglePlans))  ToggleOverlay(ref _preferences.ShowPlans, "Paths & plans");
        }

        private void NextInspectorPage()
        {
            _preferences.InspectorPage = _preferences.InspectorPage == InspectorPage.Hidden
                ? InspectorPage.Overview
                : _preferences.InspectorPage + 1;
            SpectatorSave.SavePreferences(_preferences);
            if (_inspected < 0) Toast($"Inspector: {_preferences.InspectorPage}  ·  watch or click an agent to inspect it");
            _nextHud = 0f;
        }

        private void ToggleOverlay(ref bool shown, string name)
        {
            shown = !shown;
            SpectatorSave.SavePreferences(_preferences);
            Toast($"{name} overlay {(shown ? "on" : "off")}");
            RefreshOverlayText();
        }

        private bool Pressed(SpectatorAction action) => _input.WasPressed(action);

        private void ShowOverview()
        {
            _followed = -1;
            _controller.ShowOverview();
            Toast("Whole arena");
            _nextHud = 0f;
        }

        private void FlyFree(string message)
        {
            _followed = -1;
            _controller.FlyFree();
            Toast(message);
            _nextHud = 0f;
        }

        private void Watch(int index)
        {
            if (index < 0 || index >= _watchable.Length || _watchable[index] == null)
            {
                Toast($"There is no agent {index + 1}");
                return;
            }

            _followed = index;
            Inspect(index);
            _controller.Follow();
            Toast($"Watching {AgentName(index)}");
            _nextHud = 0f;
        }

        private void Cycle(int direction)
        {
            int count = _watchable.Length;
            if (count == 0) return;
            int from = _followed >= 0 ? _followed : direction > 0 ? -1 : 0;
            Watch(((from + direction) % count + count) % count);
        }

        // Stays on through overview and free flight, so overlays and the panel can be watched
        // from anywhere. −1 = nobody.
        private void Inspect(int index)
        {
            _inspected = index;
            _inspector.Inspect(index >= 0 ? _watchable[index] : null);
            _nextHud = 0f;
        }

        // An agent, or its label, close to where the pointer clicked; clicking empty ground
        // stops inspecting. Clicks on the HUD don't count.
        private void TryPick()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 pointer = _input.PointerPosition;
            float   radius  = PickRadius * Screen.height / 1080f;
            float   best    = radius * radius;
            int     picked  = -1;
            for (int i = 0; i < _watchable.Length; i++)
            {
                if (_watchable[i] == null) continue;
                Vector3 feet = _watchable[i].transform.position;
                float distance = Mathf.Min(ScreenDistanceSq(feet + Vector3.up * FocusHeight, pointer),
                                           ScreenDistanceSq(feet + Vector3.up * _labelHeight, pointer));
                if (distance >= best) continue;
                best   = distance;
                picked = i;
            }
            if (picked >= 0) Watch(picked);
            else if (_inspected >= 0)
            {
                Toast("Stopped inspecting");
                Inspect(-1);
            }
        }

        private float ScreenDistanceSq(Vector3 world, Vector2 pointer)
        {
            Vector3 screen = _camera.WorldToScreenPoint(world);
            return screen.z <= 0f ? float.MaxValue : (new Vector2(screen.x, screen.y) - pointer).sqrMagnitude;
        }

        // --- Camera ---------------------------------------------------------------------

        private void Steer(float dt)
        {
            bool blocked = _settingsPanel.IsOpen;
            bool looking = !blocked && _input.IsHeld(SpectatorAction.Look);
            CaptureCursor(looking);

            Vector2 look = Vector2.zero;
            if (looking) look += _input.PointerDelta * _preferences.LookSensitivity;
            if (!blocked) look += _input.StickLook * (_inputSettings.StickLookSpeed * dt);
            if (_preferences.InvertLook) look.y = -look.y;

            SpectatorMotion motion = blocked
                ? default
                : new SpectatorMotion(_input.Move, look, _input.Zoom, _input.IsHeld(SpectatorAction.Fast));

            SpectatorCameraMode before = _controller.Mode;
            _controller.Tick(motion, FollowPoint(), _preferences.MoveSpeed, _preferences.Smoothing, dt);
            if (before != _controller.Mode)
            {
                _followed = -1;
                Toast($"Free camera  ·  {Prompt(SpectatorAction.Overview)} for the whole arena");
                _nextHud = 0f;
            }

            transform.SetPositionAndRotation(_controller.Position, _controller.Rotation);
        }

        private Vector3? FollowPoint()
        {
            if (_followed < 0 || _followed >= _watchable.Length || _watchable[_followed] == null) return null;
            return _watchable[_followed].transform.position + Vector3.up * FocusHeight;
        }

        // Hidden and held in place while looking around, so the mouse can turn freely.
        private void CaptureCursor(bool capture)
        {
            if (_cursorCaptured == capture) return;
            _cursorCaptured = capture;
            CursorLock.Set(capture);
        }

        private void ApplyPreferences()
        {
            _labels.SetScale(_preferences.LabelScale);
            _labels.SetVisible(_spectating && _preferences.ShowLabels);
        }

        // --- HUD ------------------------------------------------------------------------

        private void Toast(string message) => _hud.Toast(message, Time.unscaledTime);

        private void RefreshHud(float now)
        {
            _nextHud = now + HudRefresh;
            _hud.SetMode(ModeText());
            _hud.SetTime(_time.IsPaused ? "<color=#FFB347><b>PAUSED</b></color>" : $"Game speed <b>{SpeedText()}</b>");
            _hud.SetHint(_hintText);
            _hud.SetOverlays(_overlayText);
            _hud.SetHelp(_helpText, _preferences.ShowHelp && !_settingsPanel.IsOpen);

            string page = _inspected >= 0 && !_settingsPanel.IsOpen ? _inspector.Describe(_inspected + 1, _preferences.InspectorPage) : null;
            if (page != null) _hud.ShowInspector(page);
            else _hud.HideInspector();
        }

        private string ModeText() => _controller.Mode switch
        {
            SpectatorCameraMode.Follow => _followed >= 0
                ? $"<b>WATCHING</b>   <color=#FFD24A>{_followed + 1}</color>  {AgentName(_followed)}"
                : "<b>WATCHING</b>",
            SpectatorCameraMode.Free => $"<b>FREE CAMERA</b>   <color=#9AA0B0>fly speed ×{_controller.FreeSpeed:0.##}</color>{InspectingText()}",
            _ => $"<b>WHOLE ARENA</b>   <color=#9AA0B0>{_watchable.Length} agents</color>{InspectingText()}",
        };

        private string InspectingText() => _inspected >= 0
            ? $"   <color=#9AA0B0>inspecting</color> <color=#FFD24A>{_inspected + 1}</color>"
            : "";

        private string SpeedText() => _time.Speed >= 1f ? $"{_time.Speed:0}×" : $"{_time.Speed:0.##}×";

        private string AgentName(int index)
        {
            EnemyAI ai = _watchable[index];
            return ai != null && ai.Health != null ? ai.Health.DisplayName : $"Agent {index + 1}";
        }

        private string Prompt(SpectatorAction action) => _inputSettings.PromptFor(action);

        // Help, hint and overlay text come from the live bindings, so they follow any rebinding.
        private void OnBindingsChanged()
        {
            _helpText = SpectatorHelpText.Controls(_inputSettings);
            _hintText = SpectatorHelpText.Hint(_inputSettings);
            RefreshOverlayText();
        }

        private void RefreshOverlayText()
        {
            _overlayText = SpectatorOverlays.Status(_preferences, _inputSettings);
            _nextHud = 0f;
        }

        private Material OverlayMaterial()
        {
            if (_overlayMaterial != null) return _overlayMaterial;
            Shader shader = Shader.Find("Sprites/Default");
            return shader != null ? new Material(shader) { name = "Spectator Overlays" } : null;
        }
    }
}
