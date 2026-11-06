using UnityEngine;
using UnityEngine.Audio;

namespace CGD.Audio
{
    // A named situation the whole mix reacts to (paused, low health, stunned): the mixer
    // snapshot to blend to while it lasts. When several are active at once the highest
    // priority wins, so a pause always muffles the world, whatever else is going on.
    [CreateAssetMenu(fileName = "NewMixState", menuName = "CGD/Audio/Mix State")]
    public class MixState : ScriptableObject
    {
        [SerializeField] private AudioMixerSnapshot _snapshot;
        [Tooltip("Highest active priority wins. Suggested: low health 10, stunned 20, paused 100")]
        [SerializeField, Min(0)] private int _priority;
        [Tooltip("Seconds to blend into the snapshot, and back out again when the state ends")]
        [SerializeField, Min(0f)] private float _fadeTime = 0.3f;

        public AudioMixerSnapshot Snapshot => _snapshot;
        public int                Priority => _priority;
        public float              FadeTime => _fadeTime;
    }
}
