using UnityEngine;
using CGD.Audio;
using CGD.Settings;

namespace CGD.Director
{
    // Plays the director's adaptive score (MusicSettings): every stem loops in sync on its
    // own 2D source, each fading toward the volume its MusicLayer asks for at the director's
    // phase and intensity, scaled by the player's music volume. Stingers mark the peak and
    // the calm after it. Keeps playing through the pause menu.
    public class DirectorMusic : MonoBehaviour
    {
        // Lead time so every stem starts on the same DSP sample.
        private const double ScheduleLead = 0.1;

        [SerializeField] private MusicSettings _settings;
        [SerializeField] private GameDirector  _director;

        private AudioSource[] _layers;
        private float[]       _levels;
        private AudioSource   _stingers;

        private void Awake()
        {
            if (_settings == null || _director == null)
            {
                Debug.LogError($"{name}: DirectorMusic needs MusicSettings and the GameDirector.", this);
                enabled = false;
                return;
            }

            MusicLayer[] layers = _settings.Layers;
            _layers = new AudioSource[layers.Length];
            _levels = new float[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].Clip != null) _layers[i] = CreateSource($"Layer {i}", layers[i].Clip, loop: true, volume: 0f);
            _stingers = CreateSource("Stingers", null, loop: false, volume: 1f);
        }

        private void Start()
        {
            if (_layers == null) return;

            double start = AudioSettings.dspTime + ScheduleLead;
            foreach (AudioSource layer in _layers)
                if (layer != null) layer.PlayScheduled(start);
        }

        private void OnEnable()
        {
            if (_layers != null) _director.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (_layers != null) _director.PhaseChanged -= OnPhaseChanged;
        }

        private void Update()
        {
            MusicLayer[] layers = _settings.Layers;
            float volume = GameSettings.Volume(AudioCategory.Music);
            float step   = _settings.FadeSpeed * Time.unscaledDeltaTime;

            for (int i = 0; i < _layers.Length; i++)
            {
                if (_layers[i] == null) continue;

                float target = layers[i].TargetVolume(_director.Phase, _director.Intensity);
                _levels[i] = Mathf.MoveTowards(_levels[i], target, step);
                _layers[i].volume = _levels[i] * volume;
            }
        }

        // The session's opening calm enters Relax from itself and gets no stinger.
        private void OnPhaseChanged(PacingPhase previous, PacingPhase next)
        {
            if (previous == next) return;

            AudioClip stinger = _settings.StingerFor(next);
            if (stinger != null) _stingers.PlayOneShot(stinger, _settings.StingerVolume * GameSettings.Volume(AudioCategory.Music));
        }

        private AudioSource CreateSource(string sourceName, AudioClip clip, bool loop, float volume)
        {
            var holder = new GameObject(sourceName);
            holder.transform.SetParent(transform, false);

            var source = holder.AddComponent<AudioSource>();
            source.clip                = clip;
            source.loop                = loop;
            source.playOnAwake         = false;
            source.spatialBlend        = 0f;
            source.volume              = volume;
            source.ignoreListenerPause = true;
            return source;
        }
    }
}
