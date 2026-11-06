using System;
using UnityEngine;
using CGD.Audio;

namespace CGD.Enemies
{
    // One kind of bark in an EnemyBarkSet: the voice lines for it and how often it is said.
    [Serializable]
    public class BarkLine
    {
        [SerializeField] private BarkKind  _kind;
        [Tooltip("Lines picked at random each time. Empty = this kind is never said")]
        [SerializeField] private SoundBank _sound;
        [Tooltip("Share of the times it could say this that it actually does")]
        [SerializeField, Range(0f, 1f)] private float _chance = 1f;

        public BarkKind  Kind   => _kind;
        public SoundBank Sound  => _sound;
        public float     Chance => _chance;
    }
}
