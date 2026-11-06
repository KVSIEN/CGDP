using UnityEngine;
using UnityEngine.Audio;

namespace CGD.Audio
{
    // Where the project's AudioMixer is wired to the game: which group each volume slider
    // owns, which exposed parameter carries that slider's volume, and the snapshot the mix
    // rests in. Everything here is optional — without it sounds fall back to scaling their
    // own volume, so the game plays the same, just without mixer effects.
    //
    // Mixer layout this expects (see SETUP.md):
    //   Master ─┬─ Effects    (Attenuation exposed as EffectsVolume)
    //           ├─ Music      (Attenuation exposed as MusicVolume)
    //           └─ Interface  (Attenuation exposed as InterfaceVolume)
    // Snapshots must leave those three Attenuation values alone: the settings menu owns
    // them. Snapshots shape the mix through other effects (low-pass, ducking, send levels).
    [CreateAssetMenu(fileName = "AudioMixerSettings", menuName = "CGD/Audio/Mixer Settings")]
    public class AudioMixerSettings : ScriptableObject
    {
        [SerializeField] private AudioMixer _mixer;
        [Tooltip("The snapshot the mix returns to when no MixState is active")]
        [SerializeField] private AudioMixerSnapshot _defaultSnapshot;

        [Header("Effects slider")]
        [SerializeField] private AudioMixerGroup _effectsGroup;
        [SerializeField] private string          _effectsParameter = "EffectsVolume";

        [Header("Music slider")]
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private string          _musicParameter = "MusicVolume";

        [Header("Interface slider")]
        [SerializeField] private AudioMixerGroup _interfaceGroup;
        [SerializeField] private string          _interfaceParameter = "InterfaceVolume";

        public AudioMixer         Mixer           => _mixer;
        public AudioMixerSnapshot DefaultSnapshot => _defaultSnapshot;

        public AudioMixerGroup GroupFor(AudioCategory category) => category switch
        {
            AudioCategory.Music     => _musicGroup,
            AudioCategory.Interface => _interfaceGroup,
            _                       => _effectsGroup,
        };

        public string ParameterFor(AudioCategory category) => category switch
        {
            AudioCategory.Music     => _musicParameter,
            AudioCategory.Interface => _interfaceParameter,
            _                       => _effectsParameter,
        };
    }
}
