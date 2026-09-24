using UnityEngine;
using CGD.Audio;

namespace CGD.Feedback
{
    // One reusable response to a game event, combining any of: a message in the
    // notification feed, a screen flash, controller vibration, camera shake and a sound.
    // "Kill confirmed", "low health", "quest complete" are each one of these, so how
    // the game reacts is tuned in assets rather than in the code that raises it.
    [CreateAssetMenu(fileName = "FeedbackPreset", menuName = "CGD/Feedback/Feedback Preset")]
    public class FeedbackPreset : ScriptableObject
    {
        [Header("Message")]
        [Tooltip("Empty = no message. {0} is replaced by the caller's detail (an enemy or quest name).")]
        [SerializeField] private string _message;
        [SerializeField] private NotificationStyle _style;

        [Header("Screen Flash")]
        [Tooltip("Alpha 0 = no flash")]
        [SerializeField] private Color _flashColor = Color.clear;
        [SerializeField, Min(0f)] private float _flashDuration = 0.25f;

        [Header("Controller Vibration")]
        [SerializeField, Range(0f, 1f)] private float _lowFrequency;
        [SerializeField, Range(0f, 1f)] private float _highFrequency;
        [SerializeField, Min(0f)] private float _vibrationDuration = 0.1f;

        [Header("Camera")]
        [SerializeField, Range(0f, 1f)] private float _cameraTrauma;

        [Header("Audio")]
        [SerializeField] private SoundBank _sound;

        public bool              HasMessage        => !string.IsNullOrEmpty(_message);
        public NotificationStyle Style             => _style;
        public bool              HasFlash          => _flashColor.a > 0f && _flashDuration > 0f;
        public Color             FlashColor        => _flashColor;
        public float             FlashDuration     => _flashDuration;
        public bool              HasVibration      => (_lowFrequency > 0f || _highFrequency > 0f) && _vibrationDuration > 0f;
        public float             LowFrequency      => _lowFrequency;
        public float             HighFrequency     => _highFrequency;
        public float             VibrationDuration => _vibrationDuration;
        public float             CameraTrauma      => _cameraTrauma;
        public SoundBank         Sound             => _sound;

        public string FormatMessage(string detail) =>
            string.IsNullOrEmpty(detail) || !_message.Contains("{0}") ? _message : _message.Replace("{0}", detail);
    }
}
