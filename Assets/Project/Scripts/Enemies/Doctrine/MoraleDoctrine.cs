using System;
using UnityEngine;

namespace CGD.Enemies
{
    // What shakes a faction's nerve, what steadies it and what it does once it breaks.
    // Everything scales the numbers of the enemy's PerceptionProfile, so the profile still
    // sets the baseline for an enemy type and the doctrine says how its faction differs.
    // The defaults change nothing.
    [Serializable]
    public sealed class MoraleDoctrine
    {
        // The most that other kin steadying it can add up to.
        private const int   MaxSteadyingKin = 3;
        // However little it cares about a lowly ally, a body still counts for something.
        private const float MinRankScale    = 0.1f;

        [Header("Shocks")]
        [Tooltip("Scales the morale lost when it sees an ally go down")]
        [SerializeField, Min(0f)] private float _allyDownScale = 1f;
        [Tooltip("Scales the morale lost when its own health first drops low")]
        [SerializeField, Min(0f)] private float _hurtScale = 1f;
        [Tooltip("Per tier a fallen ally stood above it (or below, taking away), the share added to that loss. Herds and chains of command feel a leader fall; 0 = every ally counts the same")]
        [SerializeField, Min(0f)] private float _rankSensitivity;

        [Header("Recovery")]
        [Tooltip("Scales the profile's morale regained per second")]
        [SerializeField, Min(0f)] private float _recoveryScale = 1f;
        [Tooltip("Metres: kin this close steady it")]
        [SerializeField, Min(0f)] private float _kinSteadiesRadius = 15f;
        [Tooltip("Morale regained per second for each of its kin close by (up to three): strength in numbers")]
        [SerializeField, Min(0f)] private float _kinSteadiesRecovery;
        [Tooltip("Its target is this weak (share of health and shield) or weaker: it smells victory")]
        [SerializeField, Range(0f, 1f)] private float _targetWeakBelow = 0.5f;
        [Tooltip("Morale regained per second in a fight against a weak target")]
        [SerializeField, Min(0f)] private float _targetWeakRecovery;

        [Header("Pressure (in a fight)")]
        [Tooltip("Morale lost per second for every hostile it senses beyond its kin close by and itself: it knows the odds")]
        [SerializeField, Min(0f)] private float _outnumberedDrain;
        [Tooltip("Morale lost per second while its shield is gone (for enemies that have one): the shield is its resolve")]
        [SerializeField, Min(0f)] private float _brokenShieldDrain;

        [Header("Breaking and rallying")]
        [Tooltip("Scales the profile's line below which it retreats (under 1 = it holds out longer)")]
        [SerializeField, Min(0.1f)] private float _retreatBelowScale = 1f;
        [Tooltip("Scales the profile's line above which it fights again")]
        [SerializeField, Min(0.1f)] private float _rallyAboveScale = 1f;

        [Header("When it breaks")]
        [SerializeField] private RetreatDestination _retreatTo = RetreatDestination.Cover;
        [Tooltip("Share of its chase speed it falls back at (under 1 = an unhurried withdrawal)")]
        [SerializeField, Range(0.2f, 1.5f)] private float _retreatSpeedScale = 1f;

        public float KinSteadiesRadius        => _kinSteadiesRadius;
        public bool  IsSteadiedByKin          => _kinSteadiesRecovery > 0f;
        public float RetreatBelowScale        => _retreatBelowScale;
        public float RallyAboveScale          => _rallyAboveScale;
        public RetreatDestination RetreatTo   => _retreatTo;
        public float RetreatSpeedScale        => _retreatSpeedScale;
        public float HurtLoss(float profileLoss) => profileLoss * _hurtScale;

        // Morale lost on seeing an ally fall, by how far above or below this enemy it ranked.
        public float AllyDownLoss(float profileLoss, int fallenTier, int ownTier) =>
            profileLoss * _allyDownScale * Mathf.Max(MinRankScale, 1f + _rankSensitivity * (fallenTier - ownTier));

        // Net morale per second (negative = draining). Pressure only applies in a fight.
        public float RecoveryPerSecond(float profileRecovery, int kinNear, bool inFight, bool targetWeak, int hostilesSensed, bool shieldBroken)
        {
            float rate = profileRecovery * _recoveryScale + _kinSteadiesRecovery * Mathf.Min(kinNear, MaxSteadyingKin);
            if (!inFight) return rate;

            if (targetWeak) rate += _targetWeakRecovery;
            rate -= _outnumberedDrain * Mathf.Max(0, hostilesSensed - (kinNear + 1));
            if (shieldBroken) rate -= _brokenShieldDrain;
            return rate;
        }

        public bool IsTargetWeak(float targetStrength) => targetStrength <= _targetWeakBelow;
    }
}
