using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Animation
{
    // The one receiver for Animation Events. Put it on the same GameObject as the Animator
    // (Unity calls event functions there) and add events to clips with:
    //   Function: OnAnimationCue    String: a cue name from AnimationCue (e.g. Footstep)
    // Everything that reacts to a cue subscribes to CueRaised instead of owning its own
    // event function, so clips stay independent of who is listening.
    public class AnimationEventRelay : MonoBehaviour
    {
        private static readonly Dictionary<string, AnimationCue> CuesByName = BuildLookup();

        [Tooltip("Events from a clip weighing less than this are ignored. Stops a blend tree or crossfade " +
                 "from raising each footstep twice")]
        [SerializeField, Range(0f, 1f)] private float _minClipWeight = 0.5f;

        public event Action<AnimationCue> CueRaised;

        // Called by Animation Events, not by code.
        public void OnAnimationCue(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < _minClipWeight) return;

            if (CuesByName.TryGetValue(animationEvent.stringParameter, out AnimationCue cue))
            {
                CueRaised?.Invoke(cue);
                return;
            }

#if UNITY_EDITOR
            // A misspelled cue would otherwise fail silently, which is hard to trace back to a clip.
            Debug.LogWarning($"{name}: unknown animation cue '{animationEvent.stringParameter}' in clip " +
                             $"'{animationEvent.animatorClipInfo.clip?.name}'.", this);
#endif
        }

        private static Dictionary<string, AnimationCue> BuildLookup()
        {
            var lookup = new Dictionary<string, AnimationCue>(StringComparer.OrdinalIgnoreCase);
            foreach (AnimationCue cue in Enum.GetValues(typeof(AnimationCue)))
                lookup[cue.ToString()] = cue;
            return lookup;
        }
    }
}
