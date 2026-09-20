using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CGD.Audio
{
    // Pool of reusable AudioSources for one-shot world sounds. Sources return to the
    // pool once they finish playing (checked each frame — no coroutine per sound).
    public class AudioPool : MonoBehaviour
    {
        public static AudioPool Instance { get; private set; }

        [SerializeField] private int _initialSize = 16;

        private readonly Stack<AudioSource> _available = new();
        private readonly List<AudioSource>  _playing   = new();

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < _initialSize; i++)
                _available.Push(CreateSource());
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            for (int i = _playing.Count - 1; i >= 0; i--)
            {
                AudioSource source = _playing[i];
                if (source.isPlaying) continue;

                source.clip = null;
                _available.Push(source);

                // Swap-remove: order doesn't matter.
                int last = _playing.Count - 1;
                _playing[i] = _playing[last];
                _playing.RemoveAt(last);
            }
        }

        public void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f,
            float spatialBlend = 1f, AudioMixerGroup mixerGroup = null)
        {
            if (clip == null) return;

            var source = _available.Count > 0 ? _available.Pop() : CreateSource();
            source.transform.position     = position;
            source.clip                   = clip;
            source.volume                 = volume;
            source.pitch                  = pitch;
            source.spatialBlend           = spatialBlend;
            source.outputAudioMixerGroup  = mixerGroup;
            source.Play();
            _playing.Add(source);
        }

        private AudioSource CreateSource()
        {
            var go  = new GameObject("PooledAudio");
            go.transform.SetParent(transform);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.spatialBlend = 1f;
            src.rolloffMode  = AudioRolloffMode.Linear;
            src.minDistance   = 1f;
            src.maxDistance   = 50f;
            return src;
        }
    }
}
