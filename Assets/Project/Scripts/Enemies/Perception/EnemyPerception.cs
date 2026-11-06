using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Stealth;
using CGD.Targeting;

namespace CGD.Enemies
{
    // What an enemy knows, built only from what it has sensed:
    //   sight    builds awareness over time (faster up close, in the centre of its view, at a
    //            moving or standing target, and once it's already looking); anything in view
    //            very close is noticed at once. Rays go to the chest and head, so low cover
    //            hides a crouching target. Stealth and smoke apply.
    //   hearing  footsteps (sprinting loud, crouching quiet, standing still silent) and
    //            noises (gunfire, swings, explosions); walls muffle them, and a heard position
    //            is only roughly right. Hearing alone never makes it certain.
    //   touch    a hostile close enough to bump into; being hit gives the attacker's rough spot.
    //   memory   where it last saw its target and which way it was going. When the target
    //            slips out of sight it guesses a little ahead along that path — the player can
    //            double back to fool it — and slowly calms down if nothing new turns up.
    //   allies   spotting a hostile shouts a callout nearby allies can hear; allies' gunfire
    //            draws them in too.
    // The real position of a target is read only while it is actually sensed.
    //
    // Cost: senses are sampled every PerceptionProfile.SenseInterval (staggered per enemy)
    // with at most a few raycasts per candidate; noises and hits are pushed in as they happen.
    public class EnemyPerception
    {
        private const float EyeHeight       = 1.5f;
        private const int   MaxCandidates   = 3;
        private const float MaxGuessSpeed   = 8f;
        private const float DefaultLoudness = 0.6f;   // footsteps of a hostile without Stealthable
        private const float TrackingGap     = 0.5f;   // seconds between sightings that still give a velocity

        private static PerceptionProfile _fallbackProfile;
        private static readonly Dictionary<HealthManager, Collider> Bodies = new();

        private readonly EnemyAI           _owner;
        private readonly Transform         _self;
        private readonly EnemyData         _data;
        private readonly PerceptionProfile _profile;
        private readonly LayerMask         _obstacleMask;
        private readonly Team              _team;
        private readonly TargetFilter        _hostiles;
        private readonly List<HealthManager> _candidates = new();
        private readonly AwarenessMeter      _meter;

        private float   _clock;
        private float   _nextSample;
        private float   _lastSample;
        private float   _lastSensedAt = float.NegativeInfinity;
        private Vector3 _lastSeenPosition;
        private Vector3 _lastSeenVelocity;
        private bool    _hasLead;
        private bool    _hunting;   // it knows a hostile is about, not just that something seemed off

        // What a look at one candidate turned up.
        private struct Stimulus
        {
            public bool  Seen;
            public bool  Instant;
            public float Gain;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Bodies.Clear();
            _fallbackProfile = null;
        }

        public EnemyPerception(EnemyAI owner, EnemyData data, LayerMask targetMask, LayerMask obstacleMask, Team team)
        {
            _owner        = owner;
            _self         = owner.transform;
            _data         = data;
            _profile      = data.Senses != null ? data.Senses : FallbackProfile;
            _obstacleMask = obstacleMask;
            _team         = team;
            _hostiles     = new TargetFilter(TargetAffiliation.Enemies, team, null, targetMask);
            _meter        = new AwarenessMeter(_profile.SuspiciousThreshold);
            LastKnownPosition = _self.position;
            LookPosition      = _self.position;
            // Spread enemies over the sampling interval so they don't all raycast on one frame.
            _nextSample = Random.value * _profile.SenseInterval;
        }

        public PerceptionProfile Profile => _profile;

        // The hostile it is focused on. Known by identity; where it is, only while sensed.
        public HealthManager  Target { get; private set; }
        public AwarenessLevel Level  { get; private set; }
        public float          Awareness => _meter.Value;

        // Engaged: certain of a hostile and sensing it right now.
        public bool IsTargetDetected => Level == AwarenessLevel.Engaged;
        public bool IsSensing        => Target != null && _clock - _lastSensedAt <= _profile.SightLossGrace;

        // Where it believes the hostile (or the disturbance) is: what it last sensed, or its guess.
        public Vector3 LastKnownPosition { get; private set; }
        // Where the last thing it noticed came from — what it turns to look at.
        public Vector3 LookPosition      { get; private set; }
        // Which way a lost target seemed to be heading (zero if unknown) — searches lean that way.
        public Vector3 SearchDirection   { get; private set; }
        // Where to aim or measure from: the target itself while sensed, otherwise the belief.
        public Vector3 TargetPosition    => IsSensing ? Target.transform.position : LastKnownPosition;

        // A lead (something new to look into) not yet consumed — read without consuming it.
        public bool HasLead => _hasLead;

        private static PerceptionProfile FallbackProfile =>
            _fallbackProfile != null ? _fallbackProfile : _fallbackProfile = ScriptableObject.CreateInstance<PerceptionProfile>();

        public void Tick(float deltaTime)
        {
            _clock += deltaTime;
            if (Target != null && Target.IsDead) LoseTarget();
            if (_clock < _nextSample) return;

            float elapsed = _clock - _lastSample;
            _lastSample = _clock;
            _nextSample = _clock + _profile.SenseInterval;
            Sample(elapsed);
        }

        // True once per new lead (something seen, heard or felt worth a look) since the last call.
        public bool ConsumeLead()
        {
            bool had = _hasLead;
            _hasLead = false;
            return had;
        }

        // Calms down completely and forgets: after a fruitless search, or on respawn.
        public void LoseTarget()
        {
            Target   = null;
            _hunting = false;
            _hasLead = false;
            _lastSensedAt = float.NegativeInfinity;
            SearchDirection = Vector3.zero;
            _meter.Clear();
            Level = AwarenessLevel.Unaware;
        }

        // --- Pushed stimuli ---------------------------------------------------------

        public void OnNoise(NoiseEvent noise)
        {
            if (noise.Source.Owner == _self.gameObject || IsTargetDetected) return;

            float distance = Vector3.Distance(noise.Position, _self.position);
            if (!CanHear(noise.Position, distance, noise.Radius)) return;

            if (noise.Source.Team == _team)
            {
                // An ally is fighting: worth going to see.
                _meter.RaiseToSuspicious();
                _hunting = true;
            }
            else
            {
                float closeness = 1f - distance / noise.Radius;
                _meter.Raise(_profile.NoiseAwareness * (0.5f + 0.5f * closeness), _profile.HearingCeiling);
            }

            if (_meter.IsSuspicious) Lead(noise.Position + Error(distance, _profile.HearingError));
            UpdateLevel();
        }

        public void OnHit(DamageInfo hit)
        {
            if (!IsHostile(hit.Source) || hit.Source.Owner == null || IsTargetDetected) return;

            Vector3 attacker = hit.Source.Owner.transform.position;
            float   distance = Vector3.Distance(attacker, _self.position);

            _meter.Raise(_profile.HitAwareness, 0.95f);
            _hunting = true;
            if (Target == null) Target = hit.Source.Owner.GetComponentInParent<HealthManager>();
            Lead(attacker + Error(distance, _profile.HitError));
            UpdateLevel();
        }

        public void OnCallout(EnemyCallout callout)
        {
            if (callout.Caller == _owner || callout.Team != _team || IsTargetDetected) return;

            float distance = Vector3.Distance(callout.From, _self.position);
            if (!CanHear(callout.From, distance, callout.Radius)) return;

            _meter.RaiseToSuspicious(0.1f);
            _hunting = true;
            float reportedDistance = Vector3.Distance(callout.From, callout.Reported);
            Lead(callout.Reported + Error(reportedDistance, _profile.HearingError));
            UpdateLevel();
        }

        // Told where to look by the game (reinforcements sent toward the player).
        public void Alert(Vector3 position, DamageSource source)
        {
            if (IsTargetDetected) return;

            _meter.RaiseToSuspicious(0.1f);
            _hunting = true;
            Lead(position);
            UpdateLevel();
        }

        // --- Sampling -------------------------------------------------------------

        private void Sample(float elapsed)
        {
            HealthManager best = null;
            Stimulus bestStimulus = default;
            float bestScore = 0f;

            float searchRange = Mathf.Max(_data.SightRange, _data.HearingRadius * 2f);
            TargetQuery.Sphere(_self.position, searchRange, _hostiles, _candidates);
            TargetQuery.SortByDistance(_candidates, _self.position);

            for (int i = 0; i < _candidates.Count && i < MaxCandidates; i++)
            {
                HealthManager candidate = _candidates[i];
                Stimulus stimulus = Sense(candidate);

                float score = stimulus.Instant ? float.MaxValue : stimulus.Gain * (stimulus.Seen ? 2f : 1f);
                if (candidate == Target) score *= 1.25f;   // keep focus on whoever it was already tracking
                if (score <= bestScore) continue;

                best = candidate;
                bestStimulus = stimulus;
                bestScore = score;
            }

            // Calms down only once there's nothing at all — not while a target is just behind a pillar.
            if (best == null)            { if (!IsSensing) _meter.Decay(_profile.DecayRate * elapsed); }
            else if (bestStimulus.Seen)  Observe(best, bestStimulus, elapsed);
            else                         HearFootsteps(best, bestStimulus, elapsed);

            UpdateLevel();
        }

        private Stimulus Sense(HealthManager candidate)
        {
            float distance = Vector3.Distance(candidate.transform.position, _self.position);
            if (distance <= _profile.TouchRadius) return new Stimulus { Seen = true, Instant = true };

            if (CanSee(candidate, out float sightGain, out bool instant))
                return new Stimulus { Seen = true, Instant = instant, Gain = sightGain };

            return new Stimulus { Gain = FootstepGain(candidate, distance) };
        }

        private void Observe(HealthManager target, Stimulus stimulus, float elapsed)
        {
            Vector3 position = target.transform.position;
            if (target != Target)
            {
                Target = target;
                _lastSeenVelocity = Vector3.zero;
            }
            else if (_clock - _lastSensedAt <= TrackingGap)
            {
                Vector3 velocity = (position - _lastSeenPosition) / Mathf.Max(0.01f, _clock - _lastSensedAt);
                velocity.y = 0f;
                _lastSeenVelocity = Vector3.ClampMagnitude(Vector3.Lerp(_lastSeenVelocity, velocity, 0.5f), MaxGuessSpeed);
            }

            _lastSeenPosition = position;
            _lastSensedAt     = _clock;
            LastKnownPosition = position;
            LookPosition      = position;

            if (stimulus.Instant) _meter.Fill();
            else _meter.Raise(stimulus.Gain * elapsed * (_meter.IsSuspicious ? _profile.SearchingSightBonus : 1f));

            // A glimpse it isn't sure of yet: it wants a closer look.
            if (!_meter.IsFull && _meter.IsSuspicious) _hasLead = true;
        }

        private void HearFootsteps(HealthManager target, Stimulus stimulus, float elapsed)
        {
            // Someone is there; who and where exactly it can't tell yet.
            if (Target == null) Target = target;
            _meter.Raise(stimulus.Gain * elapsed, _profile.HearingCeiling);

            if (!_meter.IsSuspicious) return;
            float distance = Vector3.Distance(target.transform.position, _self.position);
            Lead(target.transform.position + Error(distance, _profile.HearingError));
        }

        private void UpdateLevel()
        {
            AwarenessLevel previous = Level;

            if (_meter.IsFull && IsSensing)  Level = AwarenessLevel.Engaged;
            else if (!_meter.IsSuspicious)   Level = AwarenessLevel.Unaware;
            else                             Level = _hunting ? AwarenessLevel.Searching : AwarenessLevel.Suspicious;

            if (Level == AwarenessLevel.Engaged)
            {
                _hunting = true;
                if (Stealthable.TryGet(Target, out Stealthable stealth)) stealth.NotifySpotted();
                if (previous != AwarenessLevel.Engaged) CallOut();
            }
            else if (previous == AwarenessLevel.Engaged)
            {
                OnTargetLost();
            }

            if (Level == AwarenessLevel.Unaware && _meter.Value <= 0f && Target != null) LoseTarget();
        }

        // Out of sight: guess a little ahead along the way it was going, and go looking.
        private void OnTargetLost()
        {
            Vector3 guess = _lastSeenPosition + _lastSeenVelocity * _profile.Prediction.EvaluateClamped();
            SearchDirection   = _lastSeenVelocity.sqrMagnitude > 0.25f ? _lastSeenVelocity.normalized : Vector3.zero;
            LastKnownPosition = guess;
            LookPosition      = _lastSeenPosition;
            _hasLead          = true;
        }

        private void CallOut()
        {
            EnemyCallouts.Raise(new EnemyCallout(_owner, _self.position, Target.transform.position, _team, _profile.CalloutRadius));
        }

        private void Lead(Vector3 position)
        {
            LastKnownPosition = position;
            LookPosition      = position;
            _hasLead          = true;
        }

        // --- Senses ---------------------------------------------------------------

        private bool CanSee(HealthManager target, out float gain, out bool instant)
        {
            gain    = 0f;
            instant = false;

            Vector3 eye = _self.position + Vector3.up * EyeHeight;
            AimPoints(target, out Vector3 chest, out Vector3 head);

            Vector3 toChest  = chest - eye;
            float   distance = toChest.magnitude;
            if (distance > _data.SightRange) return false;

            float halfAngle = _data.SightAngle * 0.5f;
            float angle     = Vector3.Angle(_self.forward, toChest);
            if (angle > halfAngle) return false;

            bool hasStealth = Stealthable.TryGet(target, out Stealthable stealth);
            if (hasStealth && !stealth.IsExposedTo(_self.position)) return false;

            if (!IsClear(eye, chest) && !IsClear(eye, head)) return false;

            instant = distance <= _profile.InstantSightDistance;
            gain    = Mathf.Lerp(_profile.NearSightGain, _profile.FarSightGain, distance / _data.SightRange);
            if (angle > halfAngle * _profile.CentralView) gain *= _profile.PeripheralGain;
            if (hasStealth) gain *= stealth.SightVisibility;
            return true;
        }

        private float FootstepGain(HealthManager target, float distance)
        {
            float loudness = Stealthable.TryGet(target, out Stealthable stealth) ? stealth.FootstepLoudness : DefaultLoudness;
            if (loudness <= 0f) return 0f;

            float range = _data.HearingRadius * loudness;
            if (distance > range || !CanHear(target.transform.position, distance, range)) return 0f;

            return _profile.FootstepGain * (1f - 0.5f * distance / range);
        }

        // Within range, and still within it once a wall in between has muffled it.
        private bool CanHear(Vector3 source, float distance, float range)
        {
            if (distance > range) return false;
            if (distance <= range * _profile.OccludedHearing) return true;

            Vector3 ears = _self.position + Vector3.up * EyeHeight;
            return IsClear(ears, source + Vector3.up * 0.5f);
        }

        private bool IsClear(Vector3 from, Vector3 to) =>
            !ConcealmentZone.BlocksLine(from, to) &&
            !Physics.Linecast(from, to, _obstacleMask, QueryTriggerInteraction.Ignore);

        // Chest and head of the target's body (its collider), so cover at chest height but
        // below the head doesn't hide a standing target — and does hide a crouching one.
        private static void AimPoints(HealthManager target, out Vector3 chest, out Vector3 head)
        {
            if (!Bodies.TryGetValue(target, out Collider body))
            {
                target.TryGetComponent(out body);
                Bodies[target] = body;
            }

            if (body != null && body.enabled)
            {
                Bounds bounds = body.bounds;
                chest = bounds.center;
                head  = new Vector3(bounds.center.x, bounds.max.y - 0.15f, bounds.center.z);
                return;
            }

            Vector3 feet = target.transform.position;
            chest = feet + Vector3.up;
            head  = feet + Vector3.up * 1.6f;
        }

        private bool IsHostile(DamageSource source) => source.Team != Team.None && source.Team != _team;

        // A heard or guessed position is off by up to this share of the distance to it.
        private static Vector3 Error(float distance, float share)
        {
            Vector2 offset = Random.insideUnitCircle * (distance * share);
            return new Vector3(offset.x, 0f, offset.y);
        }
    }
}
