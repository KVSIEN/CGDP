using UnityEngine;

namespace CGD.Animation
{
    // Plays the sounds an AnimationCueSoundSet maps to the cues its model's clips raise.
    // Lives next to the AnimationEventRelay (one per Animator). The player's footsteps stay
    // with PlayerFootsteps, which knows the surface underfoot — leave Footstep out of the
    // player's set.
    [RequireComponent(typeof(AnimationEventRelay))]
    public class AnimationCueAudio : MonoBehaviour
    {
        [SerializeField] private AnimationCueSoundSet _sounds;
        [Tooltip("Where sounds play from. Defaults to this object")]
        [SerializeField] private Transform _origin;

        private AnimationEventRelay _relay;

        private void Awake() => _relay = GetComponent<AnimationEventRelay>();

        private void OnEnable() => _relay.CueRaised += OnCueRaised;

        private void OnDisable() => _relay.CueRaised -= OnCueRaised;

        // Weapons swap their set in when they are drawn (see ViewmodelAnimator).
        public void SetSounds(AnimationCueSoundSet sounds) => _sounds = sounds;

        private void OnCueRaised(AnimationCue cue)
        {
            if (_sounds == null) return;
            _sounds.Play(cue, (_origin != null ? _origin : transform).position);
        }
    }
}
