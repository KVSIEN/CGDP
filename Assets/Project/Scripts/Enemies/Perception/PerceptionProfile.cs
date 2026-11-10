using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // How an enemy's senses, memory and reactions are tuned — shared by enemy types that
    // should feel alike. Ranges stay on EnemyData (sight range and angle, hearing range);
    // this asset is how quickly what they sense turns into belief, and how they act on it.
    [CreateAssetMenu(fileName = "DefaultPerceptionProfile", menuName = "CGD/Enemies/Perception Profile")]
    public class PerceptionProfile : ScriptableObject
    {
        [Header("Sampling")]
        [Tooltip("Seconds between looks and listens. Lower reacts a little faster and costs more raycasts")]
        [SerializeField, Min(0.02f)] private float _senseInterval = 0.15f;

        [Header("Sight")]
        [Tooltip("Awareness gained per second watching a target up close (a full meter = certain)")]
        [SerializeField, Min(0f)] private float _nearSightGain = 2.5f;
        [Tooltip("Awareness gained per second watching a target at the edge of sight range")]
        [SerializeField, Min(0f)] private float _farSightGain = 0.35f;
        [Tooltip("Metres: anything in view this close is noticed at once")]
        [SerializeField, Min(0f)] private float _instantSightDistance = 3f;
        [Tooltip("Share of the view cone that is sharp; outside it sight is slower")]
        [SerializeField, Range(0.05f, 1f)] private float _centralView = 0.4f;
        [Tooltip("Sight speed in the edge of the view cone")]
        [SerializeField, Range(0f, 1f)] private float _peripheralGain = 0.45f;
        [Tooltip("Sight speed while already suspicious — it is looking for something")]
        [SerializeField, Min(1f)] private float _searchingSightBonus = 1.75f;
        [Tooltip("Seconds a target may slip out of view (a pillar flicking past) before it counts as lost")]
        [SerializeField, Min(0f)] private float _sightLossGrace = 0.4f;

        [Header("Hearing")]
        [Tooltip("Awareness per second while hearing footsteps (scaled by how close)")]
        [SerializeField, Min(0f)] private float _footstepGain = 0.7f;
        [Tooltip("Awareness from one noise (gunfire, a swing, an explosion) heard up close")]
        [SerializeField, Min(0f)] private float _noiseAwareness = 0.55f;
        [Tooltip("Hearing alone can't make it certain: awareness from sound stops here")]
        [SerializeField, Range(0f, 1f)] private float _hearingCeiling = 0.9f;
        [Tooltip("Through a wall sounds carry this share of their range")]
        [SerializeField, Range(0f, 1f)] private float _occludedHearing = 0.5f;
        [Tooltip("How far off a heard position may be, as a share of the distance to it")]
        [SerializeField, Range(0f, 0.5f)] private float _hearingError = 0.15f;

        [Header("Scent")]
        [Tooltip("Metres a bleeding hostile can be tracked by smell, walls or not; 0 = no sense of smell. Works like hearing: rough positions, never certainty")]
        [SerializeField, Min(0f)] private float _scentRange;
        [Tooltip("Awareness per second from a bleeding hostile's trail (scaled by how close)")]
        [SerializeField, Min(0f)] private float _scentGain = 0.6f;

        [Header("Touch")]
        [Tooltip("Metres: a hostile this close is felt, seen or not")]
        [SerializeField, Min(0f)] private float _touchRadius = 1.3f;
        [Tooltip("Awareness from being hit; the attacker's direction is known, its spot only roughly")]
        [SerializeField, Range(0f, 1f)] private float _hitAwareness = 0.6f;
        [Tooltip("How far off the guessed attacker position may be, as a share of its distance")]
        [SerializeField, Range(0f, 0.5f)] private float _hitError = 0.25f;

        [Header("Memory")]
        [Tooltip("Awareness at which it turns suspicious")]
        [SerializeField, Range(0.05f, 0.95f)] private float _suspiciousThreshold = 0.3f;
        [Tooltip("Awareness lost per second with nothing to go on (from full: seconds of searching ≈ 0.7 / this)")]
        [SerializeField, Min(0.01f)] private float _decayRate = 0.08f;
        [Tooltip("Seconds ahead it guesses a lost target kept moving the way it was seen going")]
        [SerializeField] private FloatRange _prediction = new(0.4f, 1.4f);
        [Tooltip("Share of that guess for a target seen strafing: it tends to stop or reverse rather than keep going")]
        [SerializeField, Range(0f, 1f)] private float _strafePrediction = 0.6f;
        [Tooltip("Share of that guess for a target seen backing away while facing it: usually a short step back to cover")]
        [SerializeField, Range(0f, 1f)] private float _backpedalPrediction = 0.4f;

        [Header("Callouts")]
        [Tooltip("Metres: allies this close hear it call out a target it spots (half through walls)")]
        [SerializeField, Min(0f)] private float _calloutRadius = 18f;

        [Header("Reactions")]
        [Tooltip("Seconds it stops and stares at something it noticed before going to look")]
        [SerializeField] private FloatRange _noticePause = new(0.4f, 1f);
        [Tooltip("Spots it checks around the last known position, besides the position itself")]
        [SerializeField] private IntRange _searchPoints = new(2, 4);
        [Tooltip("Metres around the last known position it searches")]
        [SerializeField, Min(1f)] private float _searchRadius = 7f;
        [Tooltip("Seconds it stands looking around at each spot")]
        [SerializeField] private FloatRange _lookAroundPause = new(0.8f, 1.8f);
        [Tooltip("Share of chase speed used while hunting (versus patrol speed when merely curious)")]
        [SerializeField, Range(0.3f, 1f)] private float _huntSpeed = 0.75f;
        [Tooltip("Seconds it stands there after giving up a search before heading back to its post")]
        [SerializeField] private FloatRange _lostInterestPause = new(1f, 2.5f);
        [Tooltip("Seconds it stands over a target it killed before heading back")]
        [SerializeField] private FloatRange _victoryPause = new(2f, 3.5f);

        [Header("Caution (after a search that found nothing)")]
        [Tooltip("Seconds it stays on edge after giving up a search")]
        [SerializeField, Min(0f)] private float _cautionDuration = 45f;
        [Tooltip("Sight speed while on edge")]
        [SerializeField, Min(1f)] private float _cautionSightBonus = 1.4f;
        [Tooltip("Patrol speed while on edge, as a multiple of normal")]
        [SerializeField, Min(1f)] private float _cautionPatrolSpeed = 1.25f;

        [Header("Guarding (no patrol route)")]
        [Tooltip("Seconds between turns to look somewhere else (halved while on edge)")]
        [SerializeField] private FloatRange _guardLookInterval = new(3f, 7f);
        [Tooltip("Degrees either side of its post's facing it looks")]
        [SerializeField, Range(0f, 180f)] private float _guardLookAngle = 70f;

        [Header("Morale")]
        [Tooltip("Morale lost when it sees (or hears, close by) an ally go down; 1 = full morale")]
        [SerializeField, Range(0f, 1f)] private float _moraleLossAllyDown = 0.3f;
        [Tooltip("Morale lost once when its health first drops low")]
        [SerializeField, Range(0f, 1f)] private float _moraleLossHurt = 0.35f;
        [Tooltip("Morale regained per second")]
        [SerializeField, Min(0f)] private float _moraleRecovery = 0.04f;
        [Tooltip("Below this it retreats and regroups")]
        [SerializeField, Range(0f, 1f)] private float _retreatBelow = 0.35f;
        [Tooltip("Above this it is ready to fight again")]
        [SerializeField, Range(0f, 1f)] private float _rallyAbove = 0.6f;

        public float SenseInterval        => _senseInterval;
        public float NearSightGain        => _nearSightGain;
        public float FarSightGain         => _farSightGain;
        public float InstantSightDistance => _instantSightDistance;
        public float CentralView          => _centralView;
        public float PeripheralGain       => _peripheralGain;
        public float SearchingSightBonus  => _searchingSightBonus;
        public float SightLossGrace       => _sightLossGrace;
        public float FootstepGain         => _footstepGain;
        public float NoiseAwareness       => _noiseAwareness;
        public float HearingCeiling       => _hearingCeiling;
        public float OccludedHearing      => _occludedHearing;
        public float HearingError         => _hearingError;
        public float TouchRadius          => _touchRadius;
        public float HitAwareness         => _hitAwareness;
        public float HitError             => _hitError;
        public float SuspiciousThreshold  => _suspiciousThreshold;
        public float DecayRate            => _decayRate;
        public FloatRange Prediction      => _prediction;
        public float StrafePrediction     => _strafePrediction;
        public float BackpedalPrediction  => _backpedalPrediction;
        public float CalloutRadius        => _calloutRadius;
        public FloatRange NoticePause     => _noticePause;
        public IntRange SearchPoints      => _searchPoints;
        public float SearchRadius         => _searchRadius;
        public FloatRange LookAroundPause => _lookAroundPause;
        public float HuntSpeed            => _huntSpeed;
        public FloatRange LostInterestPause => _lostInterestPause;
        public FloatRange VictoryPause    => _victoryPause;
        public float CautionDuration      => _cautionDuration;
        public float CautionSightBonus    => _cautionSightBonus;
        public float CautionPatrolSpeed   => _cautionPatrolSpeed;
        public FloatRange GuardLookInterval => _guardLookInterval;
        public float GuardLookAngle       => _guardLookAngle;
        public float MoraleLossAllyDown   => _moraleLossAllyDown;
        public float MoraleLossHurt       => _moraleLossHurt;
        public float MoraleRecovery       => _moraleRecovery;
        public float RetreatBelow         => _retreatBelow;
        public float RallyAbove           => _rallyAbove;
        public float ScentRange           => _scentRange;
        public float ScentGain            => _scentGain;
    }
}
