using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // How far ahead a faction thinks: where it expects a moving target to be, and how it
    // reads the route to somewhere it has not seen yet. The defaults change nothing: it
    // heads for where the target is, aims at where it is, and walks straight there.
    [Serializable]
    public sealed class ForesightDoctrine
    {
        [Header("Pursuit and aim")]
        [Tooltip("Share of the time it needs to reach a moving target that it leads it by, running to where it will be. 0 = straight at where it is")]
        [SerializeField, Range(0f, 1.5f)] private float _interceptShare;
        [Tooltip("Metres: the furthest it will lead a target by, in a chase or with an ability")]
        [SerializeField, Min(0f)] private float _maxLead = 5f;
        [Tooltip("Share of an ability's wind-up it leads a moving target by when it aims (over 1 = it expects you to keep running). 0 = aims at where it is")]
        [SerializeField, Range(0f, 1.5f)] private float _zoneLeadShare;

        [Header("Approaching what it has not seen")]
        [Tooltip("Seconds it holds at each corner of its route, looking down the next stretch, before rounding it. 0 = it doesn't")]
        [SerializeField] private FloatRange _cornerPause = new(0f, 0f);
        [Tooltip("Share of its speed on a long clear stretch of the route (a predator's rush)")]
        [SerializeField, Range(0.5f, 2f)] private float _straightPace = 1f;
        [Tooltip("Metres: a stretch at least this long counts as a long clear one")]
        [SerializeField, Min(1f)] private float _straightLength = 8f;
        [Tooltip("Times a minute it stops dead on its way to listen")]
        [SerializeField, Min(0f)] private float _listenStopsPerMinute;
        [SerializeField] private FloatRange _listenDuration = new(1f, 2f);

        public float InterceptShare      => _interceptShare;
        public float MaxLead             => _maxLead;
        public float ZoneLeadShare       => _zoneLeadShare;
        public FloatRange CornerPause    => _cornerPause;
        public float StraightPace        => _straightPace;
        public float StraightLength      => _straightLength;
        public float ListenStopsPerMinute => _listenStopsPerMinute;
        public FloatRange ListenDuration => _listenDuration;
    }
}
