using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using CGD.Settings;

namespace CGD.Audio
{
    // Runs the AudioMixer: pushes the settings menu's volume sliders into the mixer and
    // blends between snapshots as systems request MixStates. One per scene, next to AudioPool.
    //
    // A category counts as mixer-driven only once its exposed parameter answers; until then
    // (and when no mixer is assigned) GainFor hands sounds the slider value to apply
    // themselves, so volume is never applied twice or lost.
    [DefaultExecutionOrder(-100)]
    public class AudioMixerController : MonoBehaviour
    {
        private const float SilenceDb      = -80f;
        private const float MinLinear      = 0.0001f;
        private static readonly int CategoryCount = Enum.GetValues(typeof(AudioCategory)).Length;

        public static AudioMixerController Instance { get; private set; }

        [SerializeField] private AudioMixerSettings _settings;

        private readonly List<MixStateRequest> _requests = new();
        private readonly bool[]                _driven   = new bool[CategoryCount];
        private readonly AudioMixerSnapshot[]  _blendSnapshots = new AudioMixerSnapshot[1];
        private readonly float[]               _blendWeights   = { 1f };
        private AudioMixerSnapshot _active;

        // --- Static API: safe to call with no controller in the scene ---------------

        // Ask for a MixState until the request is released. Inert without a controller.
        public static MixStateRequest Push(MixState state)
        {
            if (state == null || Instance == null) return MixStateRequest.Inert;
            return Instance.Add(state);
        }

        // The mixer group a sound of this category plays through, or null (play unrouted).
        public static AudioMixerGroup GroupFor(AudioCategory category) =>
            Instance != null && Instance._settings != null ? Instance._settings.GroupFor(category) : null;

        // The volume a sound must apply itself: 1 when the mixer already carries the slider.
        public static float GainFor(AudioCategory category) =>
            Instance != null && Instance._driven[(int)category] ? 1f : GameSettings.Volume(category);

        // --- Lifecycle -------------------------------------------------------------

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => GameSettings.Changed += ApplyVolumes;

        private void OnDisable() => GameSettings.Changed -= ApplyVolumes;

        // Mixer parameters set in Awake are unreliable, so the first write waits for Start.
        private void Start()
        {
            ApplyVolumes();
            // The mixer outlives scenes: start from the resting snapshot, not whatever the last one left.
            TransitionTo(ResolveTarget(), 0f);
        }

        // --- Volumes ---------------------------------------------------------------

        private void ApplyVolumes()
        {
            if (_settings == null || _settings.Mixer == null) return;

            foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory)))
            {
                float db = ToDecibels(GameSettings.Volume(category));
                _driven[(int)category] = _settings.Mixer.SetFloat(_settings.ParameterFor(category), db);
            }
        }

        // Sliders are linear but the mixer is logarithmic, so equal slider steps sound equal.
        private static float ToDecibels(float linear) =>
            linear <= MinLinear ? SilenceDb : Mathf.Log10(linear) * 20f;

        // --- Mix states ------------------------------------------------------------

        private MixStateRequest Add(MixState state)
        {
            var request = new MixStateRequest(this, state);
            _requests.Add(request);
            TransitionTo(ResolveTarget(), state.FadeTime);
            return request;
        }

        internal void Release(MixStateRequest request)
        {
            if (!_requests.Remove(request)) return;
            TransitionTo(ResolveTarget(), request.State.FadeTime);
        }

        // Highest priority wins; among equals the newest request.
        private AudioMixerSnapshot ResolveTarget()
        {
            MixStateRequest top = null;
            foreach (MixStateRequest request in _requests)
                if (top == null || request.State.Priority >= top.State.Priority)
                    top = request;

            AudioMixerSnapshot snapshot = top?.State.Snapshot;
            return snapshot != null ? snapshot : _settings != null ? _settings.DefaultSnapshot : null;
        }

        private void TransitionTo(AudioMixerSnapshot target, float fadeTime)
        {
            if (target == null || _settings == null || _settings.Mixer == null) return;
            if (target == _active && fadeTime > 0f) return;

            _active            = target;
            _blendSnapshots[0] = target;
            // Mixer transitions run on the audio clock, so they keep fading while the game is paused.
            _settings.Mixer.TransitionToSnapshots(_blendSnapshots, _blendWeights, fadeTime);
        }
    }
}
