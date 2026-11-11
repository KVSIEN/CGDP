using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // One thing an enemy does with its idle time: look about in some way for a while, perhaps
    // after walking a little way, perhaps saying something. A faction's idle routine is a list
    // of these (IdleDoctrine).
    [Serializable]
    public sealed class IdleBeat
    {
        [Tooltip("Just a name for the inspector")]
        [SerializeField] private string _label = "Beat";
        [SerializeField] private GazePattern _gaze = GazePattern.Scatter;
        [Tooltip("Seconds it keeps at it")]
        [SerializeField] private FloatRange _duration = new(3f, 5f);
        [Tooltip("Chance of being picked next, relative to the others (weighted order only)")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Scatter only: seconds between glances. Empty (0) = the profile's guard look interval")]
        [SerializeField] private FloatRange _lookInterval = new(0f, 0f);
        [Tooltip("Scales the profile's arc either side of its facing, up to 180°")]
        [SerializeField, Min(0.1f)] private float _arcScale = 1f;
        [Tooltip("Metres: if above 0 it first walks to a spot this near its post (a stop on a route can't)")]
        [SerializeField, Min(0f)] private float _walkRadius;
        [Tooltip("Said as the beat begins (needs a line for it in the bark set)")]
        [SerializeField] private BarkKind _bark = BarkKind.None;

        public GazePattern Gaze      => _gaze;
        public FloatRange Duration   => _duration;
        public float Weight          => _weight;
        public FloatRange LookInterval => _lookInterval;
        public float ArcScale        => _arcScale;
        public float WalkRadius      => _walkRadius;
        public BarkKind Bark         => _bark;
    }
}
