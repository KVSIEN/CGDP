using UnityEngine;
using UnityEngine.InputSystem;
using CGD.Audio;
using CGD.CameraEffects;
using CGD.UI;

namespace CGD.Feedback
{
    // Turns FeedbackBus requests into what the player sees and feels: messages in the
    // notification feed, screen flashes, gamepad vibration, camera shake and sound.
    // Every output is optional — leave a reference empty and that channel is skipped.
    public class FeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private NotificationHUD         _notifications;
        [SerializeField] private ScreenFlashHUD          _screenFlash;
        [SerializeField] private CameraEffectsController _cameraEffects;

        [Header("Vibration")]
        [SerializeField] private bool _vibration = true;
        [SerializeField, Range(0f, 2f)] private float _vibrationStrength = 1f;

        private readonly HapticMixer _haptics = new();
        private float _lastLow;
        private float _lastHigh;

        public bool VibrationEnabled
        {
            get => _vibration;
            set
            {
                _vibration = value;
                if (!value) StopVibration();
            }
        }

        private void OnEnable()  => FeedbackBus.Requested += Play;

        private void OnDisable()
        {
            FeedbackBus.Requested -= Play;
            StopVibration();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) StopVibration();
        }

        // Motors follow game time: nothing keeps buzzing while the game is paused.
        private void Update()
        {
            if (!_haptics.IsActive && _lastLow == 0f && _lastHigh == 0f) return;

            float low = 0f, high = 0f;
            if (Time.timeScale > 0f) _haptics.Tick(Time.unscaledDeltaTime, out low, out high);
            SetMotors(low, high);
        }

        private void Play(FeedbackRequest request)
        {
            if (request.Message != null && _notifications != null)
                _notifications.Show(request.Message, request.Style);

            FeedbackPreset preset = request.Preset;
            if (preset == null) return;

            float intensity = request.Intensity;

            if (preset.HasFlash && _screenFlash != null)
                _screenFlash.Flash(preset.FlashColor, preset.FlashDuration, Mathf.Clamp01(intensity));

            if (preset.HasVibration && _vibration)
                _haptics.Add(preset.LowFrequency * intensity * _vibrationStrength,
                             preset.HighFrequency * intensity * _vibrationStrength,
                             preset.VibrationDuration);

            if (preset.CameraTrauma > 0f && _cameraEffects != null)
                _cameraEffects.AddTrauma(preset.CameraTrauma * intensity);

            preset.Sound.TryPlay(transform.position);
        }

        private void SetMotors(float low, float high)
        {
            if (Mathf.Approximately(low, _lastLow) && Mathf.Approximately(high, _lastHigh)) return;

            _lastLow  = low;
            _lastHigh = high;
            Gamepad.current?.SetMotorSpeeds(low, high);
        }

        private void StopVibration()
        {
            _haptics.Clear();
            _lastLow = _lastHigh = 0f;
            Gamepad.current?.ResetHaptics();
        }
    }
}
