using System;
using UnityEngine;

namespace CGD.Enemies
{
    // A faction's own director (after Alien: Isolation): something watching over all its
    // members that knows more than any of them, and uses it sparingly. It tracks menace (how
    // hard the faction has been pressing lately) and may pull the faction back when it peaks;
    // when its members have lost all contact for a while it drip-feeds one or two calm ones a
    // vague hint of where a hostile is — an area to check, never the spot; and every time a
    // target gets away from a search it escalates, sharpening its hints. The defaults oversee
    // nothing.
    [Serializable]
    public sealed class OverseerDoctrine
    {
        [SerializeField] private bool _oversees;

        [Header("Menace")]
        [Tooltip("Menace gained per second for each member in contact (up to three)")]
        [SerializeField, Min(0f)] private float _menaceRise = 1f;
        [Tooltip("Menace lost per second while no member is in contact")]
        [SerializeField, Min(0f)] private float _menaceDecay = 0.5f;
        [SerializeField, Min(1f)] private float _menaceMax = 30f;
        [Tooltip("It pulls the faction back once menace peaks: every encounter regroups and no hints are given")]
        [SerializeField] private bool _backsOff;
        [SerializeField, Min(0f)] private float _backOffAt = 20f;
        [Tooltip("Seconds at least that it holds back")]
        [SerializeField, Min(0f)] private float _backOffSeconds = 10f;
        [Tooltip("It comes again only once menace has drained this low")]
        [SerializeField, Min(0f)] private float _resumeBelow = 8f;

        [Header("Hints")]
        [Tooltip("Seconds with no member in contact before it starts hinting")]
        [SerializeField, Min(0f)] private float _hintAfterQuiet = 25f;
        [Tooltip("Seconds between hints")]
        [SerializeField, Min(1f)] private float _hintEvery = 15f;
        [Tooltip("Metres: how far off the hinted area may be from the hostile")]
        [SerializeField, Min(1f)] private float _hintAccuracy = 12f;
        [Tooltip("Metres: hostiles further than this from every calm member are left alone")]
        [SerializeField, Min(1f)] private float _hintRange = 45f;
        [Tooltip("Calm members sent to look each time (the nearest)")]
        [SerializeField, Min(1)] private int _hintMembers = 1;

        [Header("Escalation")]
        [Tooltip("Share by which each escalation tightens the hinted area")]
        [SerializeField, Range(0f, 0.3f)] private float _sharpenPerEscalation = 0.2f;
        [SerializeField, Range(0, 5)] private int _maxEscalation = 3;

        public bool  Oversees              => _oversees;
        public float MenaceRise            => _menaceRise;
        public float MenaceDecay           => _menaceDecay;
        public float MenaceMax             => _menaceMax;
        public bool  BacksOff              => _backsOff;
        public float BackOffAt             => _backOffAt;
        public float BackOffSeconds        => _backOffSeconds;
        public float ResumeBelow           => _resumeBelow;
        public float HintAfterQuiet        => _hintAfterQuiet;
        public float HintEvery             => _hintEvery;
        public float HintRange             => _hintRange;
        public int   HintMembers           => _hintMembers;
        public int   MaxEscalation         => _maxEscalation;

        // Metres the hinted area may be off by, at this escalation (never under 2 m).
        public float HintAccuracy(int escalation) =>
            Mathf.Max(2f, _hintAccuracy * (1f - _sharpenPerEscalation * Mathf.Clamp(escalation, 0, _maxEscalation)));
    }
}
