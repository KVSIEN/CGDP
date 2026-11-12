using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // How a faction gets out of the way of something that is about to hurt it: a live grenade
    // it can see. The defaults leave an enemy standing in it, as a plain enemy does.
    [Serializable]
    public sealed class ReflexDoctrine
    {
        [Tooltip("It sees a hostile grenade coming and gets clear of the blast")]
        [SerializeField] private bool _avoidsGrenades;
        [Tooltip("Metres: it notices a grenade this close, if it can see it")]
        [SerializeField, Min(1f)] private float _noticeRange = 15f;
        [Tooltip("Seconds between noticing and moving: a cool head is quick, a slow mind is not")]
        [SerializeField] private FloatRange _reactionDelay = new(0.1f, 0.25f);
        [Tooltip("Share of its chase speed it runs at")]
        [SerializeField, Range(0.5f, 2f)] private float _speedScale = 1.1f;
        [Tooltip("Metres beyond the blast it wants to end up. Negative = it will stand at the edge and take a graze")]
        [SerializeField, Range(-2f, 5f)] private float _safetyMargin = 1.5f;
        [Tooltip("It would rather get behind something solid than run out of the radius")]
        [SerializeField] private bool _seeksCover;
        [Tooltip("It keeps its eyes on the grenade while it moves, instead of looking where it is going")]
        [SerializeField] private bool _watchesIt;
        [Tooltip("Said as it reacts (needs a line for it in the bark set)")]
        [SerializeField] private BarkKind _bark = BarkKind.None;

        public bool  AvoidsGrenades      => _avoidsGrenades;
        public float NoticeRange         => _noticeRange;
        public FloatRange ReactionDelay  => _reactionDelay;
        public float SpeedScale          => _speedScale;
        public float SafetyMargin        => _safetyMargin;
        public bool  SeeksCover          => _seeksCover;
        public bool  WatchesIt           => _watchesIt;
        public BarkKind Bark             => _bark;
    }
}
