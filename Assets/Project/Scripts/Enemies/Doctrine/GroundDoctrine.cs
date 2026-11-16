using System;
using UnityEngine;

namespace CGD.Enemies
{
    // How a faction uses the ground its enemies have learned (EnemyGround): what each kind of
    // place is worth to it, how much a place where blood was spilt puts it off, whether it
    // falls back to the best place it knows, and whether it tells its kin what it knows.
    [Serializable]
    public sealed class GroundDoctrine
    {
        [Tooltip("What a place inside a concealment zone is worth to it (0 = nothing)")]
        [SerializeField, Min(0f)] private float _hidden = 1f;
        [Tooltip("What a firing position down a long lane with cover beside it is worth")]
        [SerializeField, Min(0f)] private float _vantage = 1f;
        [Tooltip("What a corridor or aisle is worth")]
        [SerializeField, Min(0f)] private float _tight = 1f;
        [Tooltip("How strongly a place where it was hurt, or saw an ally fall, puts it off while the memory is fresh")]
        [SerializeField, Min(0f)] private float _dangerAversion = 1f;
        [Tooltip("When its nerve breaks, run to the best place it knows (falling back to cover, kin or its post when it knows none)")]
        [SerializeField] private bool _fallBackToKnownGround;
        [Tooltip("Tell kin close by about the places it knows well")]
        [SerializeField] private bool _shareWithKin = true;

        public GroundDoctrine() { }

        public GroundDoctrine(float hidden, float vantage, float tight, float dangerAversion, bool fallBack, bool share)
        {
            _hidden = hidden;
            _vantage = vantage;
            _tight = tight;
            _dangerAversion = dangerAversion;
            _fallBackToKnownGround = fallBack;
            _shareWithKin = share;
        }

        public float DangerAversion       => _dangerAversion;
        public bool  FallBackToKnownGround => _fallBackToKnownGround;
        public bool  ShareWithKin         => _shareWithKin;

        // The most a place's traits are worth (0 for a place good for nothing it values).
        public float ValueOf(PlaceTraits traits)
        {
            float value = 0f;
            if ((traits & PlaceTraits.Hidden)  != 0) value = Mathf.Max(value, _hidden);
            if ((traits & PlaceTraits.Vantage) != 0) value = Mathf.Max(value, _vantage);
            if ((traits & PlaceTraits.Tight)   != 0) value = Mathf.Max(value, _tight);
            return value;
        }
    }
}
