using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CGD.Input;
using CGD.Player;

namespace CGD.UI
{
    // Mouse/gamepad sensitivity controls of the SettingsMenu. Edits stay pending
    // until "Apply Sensitivity" pushes them to the camera and saves them.
    public class SensitivitySection
    {
        private readonly PlayerCamera _camera;

        private Slider         _mouseSlider;
        private TMP_InputField _mouseField;
        private Slider         _gamepadSlider;
        private TMP_InputField _gamepadField;

        private float _pendingMouse;
        private float _pendingGamepad;

        public SensitivitySection(RectTransform window, PlayerCamera camera)
        {
            _camera = camera;
            Build(window);
        }

        // Discards unapplied edits and shows the saved values.
        public void Load()
        {
            SettingsSave.LoadSensitivity(out _pendingMouse, out _pendingGamepad);
            _mouseSlider.SetValueWithoutNotify(_pendingMouse);
            _mouseField.SetTextWithoutNotify(_pendingMouse.ToString("F2"));
            _gamepadSlider.SetValueWithoutNotify(_pendingGamepad);
            _gamepadField.SetTextWithoutNotify(_pendingGamepad.ToString("F0"));
        }

        private void Build(RectTransform window)
        {
            var title = UIFactory.MakeText("SensitivityHeader", window);
            title.text = "── Sensitivity ──";
            title.fontSize = 13f;
            UIFactory.Place(title.rectTransform, new Vector2(10f, -20f), new Vector2(580f, 20f));

            var mouseLabel = UIFactory.MakeText("MouseLabel", window);
            mouseLabel.text = "Mouse:";
            mouseLabel.fontSize = 12f;
            UIFactory.Place(mouseLabel.rectTransform, new Vector2(10f, -44f), new Vector2(60f, 20f));

            _mouseSlider = UIFactory.MakeSlider("MouseSlider", window);
            UIFactory.Place(_mouseSlider.GetComponent<RectTransform>(), new Vector2(75f, -48f), new Vector2(330f, 16f));
            _mouseSlider.minValue = 0.1f;
            _mouseSlider.maxValue = 10f;

            _mouseField = UIFactory.MakeInputField("MouseField", window);
            UIFactory.Place(_mouseField.GetComponent<RectTransform>(), new Vector2(415f, -44f), new Vector2(70f, 20f));
            _mouseField.contentType = TMP_InputField.ContentType.DecimalNumber;

            _mouseSlider.onValueChanged.AddListener(v =>
            {
                _pendingMouse = v;
                _mouseField.SetTextWithoutNotify(v.ToString("F2"));
            });
            _mouseField.onValueChanged.AddListener(str =>
            {
                if (float.TryParse(str, out float v))
                {
                    _pendingMouse = Mathf.Clamp(v, 0.1f, 10f);
                    _mouseSlider.SetValueWithoutNotify(_pendingMouse);
                }
            });

            var gamepadLabel = UIFactory.MakeText("GamepadLabel", window);
            gamepadLabel.text = "Gamepad:";
            gamepadLabel.fontSize = 12f;
            UIFactory.Place(gamepadLabel.rectTransform, new Vector2(10f, -70f), new Vector2(60f, 20f));

            _gamepadSlider = UIFactory.MakeSlider("GamepadSlider", window);
            UIFactory.Place(_gamepadSlider.GetComponent<RectTransform>(), new Vector2(75f, -74f), new Vector2(330f, 16f));
            _gamepadSlider.minValue = 10f;
            _gamepadSlider.maxValue = 300f;

            _gamepadField = UIFactory.MakeInputField("GamepadField", window);
            UIFactory.Place(_gamepadField.GetComponent<RectTransform>(), new Vector2(415f, -70f), new Vector2(70f, 20f));
            _gamepadField.contentType = TMP_InputField.ContentType.DecimalNumber;

            _gamepadSlider.onValueChanged.AddListener(v =>
            {
                _pendingGamepad = v;
                _gamepadField.SetTextWithoutNotify(v.ToString("F0"));
            });
            _gamepadField.onValueChanged.AddListener(str =>
            {
                if (float.TryParse(str, out float v))
                {
                    _pendingGamepad = Mathf.Clamp(v, 10f, 300f);
                    _gamepadSlider.SetValueWithoutNotify(_pendingGamepad);
                }
            });

            var applyBtn = UIFactory.MakeButton("ApplyButton", window, "Apply Sensitivity", out _);
            UIFactory.Place(applyBtn.GetComponent<RectTransform>(), new Vector2(170f, -98f), new Vector2(140f, 22f));
            applyBtn.onClick.AddListener(() =>
            {
                _camera.SetSensitivity(_pendingMouse, _pendingGamepad);
                SettingsSave.SaveSensitivity(_pendingMouse, _pendingGamepad);
            });
        }
    }
}
