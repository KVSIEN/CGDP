using System;
using UnityEngine;
using CGD.Audio;

namespace CGD.Animation
{
    // Which sound each animation cue makes, for one character type or one weapon. Shared
    // by everything that uses it, so retuning "Rifle reload" is one asset. A cue may have
    // several entries to layer sounds; a cue with none stays silent.
    [CreateAssetMenu(fileName = "NewCueSoundSet", menuName = "CGD/Animation/Cue Sound Set")]
    public class AnimationCueSoundSet : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public AnimationCue Cue;
            public SoundBank    Sound;
        }

        [SerializeField] private Entry[] _entries;

        public void Play(AnimationCue cue, Vector3 position)
        {
            foreach (Entry entry in _entries)
                if (entry.Cue == cue) entry.Sound.TryPlay(position);
        }
    }
}
