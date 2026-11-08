using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Impacts;
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
    //   memory   where it last saw its target, which way it faced and how it moved (still,
    //            running, strafing, backing away — SightingMemory). When the target slips out
    //            of sight it guesses a little ahead along that path, less far for a strafing or
    //            backpedalling target and not at all for one standing still — the player can
    //            double back to fool it — and slowly calms down if nothing new turns up.
    //   allies   spotting a hostile shouts a callout nearby allies can hear; allies' gunfire
    //            draws them in too.
    //   choice   with several hostiles in its senses it fights the one it senses most strongly,
    //            weighed by its TargetPriorities: sticking with its current target, whoever
    //            just hurt it (even mid-fight with someone else), the weakest, and whoever is
    //            busy fighting someone else.
    // The real position of a target is read only while it is actually sensed.
    //
    // Cost: senses are sampled every PerceptionProfile.SenseInterval (staggered per enemy)
    // with at most a few raycasts per candidate; noises and hits are pushed in as they happen.
    public class EnemyPerception
    {
        private const float EyeHeight       = 1.5f;
        private const int   MaxCandidates   = 3;
        private const float DefaultLoudness = 0.6f;   // footsteps of a hostile without Stealthable
        private const float DeathHearingRange = 10f;  // an ally going down is heard this far
        private const int   MaxBodyChecks   = 2;      // raycasts per sample spent looking for bodies
        private const float NearMissRadius  = 2.5f;   // a hostile bullet landing this close is felt
        private const float UnderFireMemory = 1.5f;   // seconds "under fire" lasts after the last shot
        private const float AimedAtAngle    = 5f;     // degrees: a target facing it this squarely is aiming at it

        private static PerceptionProfile _fallbackProfile;
        private static readonly Dictionary<HealthManager, Collider> Bodies = new();
        // Which hostiles are enemies with minds of their own (null for the player), to tell
        // whether one is busy fighting someone else.
        private static readonly Dictionary<HealthManager, EnemyAI> Brains = new();

        private readonly EnemyAI           _owner;
        private readonly Transform         _self;
        private readonly EnemyData         _data;
        private readonly PerceptionProfile _profile;
        private readonly LayerMask         _obstacleMask;
        private readonly Team              _team;
        private readonly TargetFilter        _hostiles;
        private readonly List<HealthManager> _candidates = new();
        private readonly AwarenessMeter      _meter;
        private readonly SightingMemory      _sighting = new();
        private readonly TargetWeights       _weights;
        private readonly float               _retaliationMemory;

        private float   _clock;
        private float   _nextSample;
        private float   _lastSample;
        private float   _lastSensedAt = float.NegativeInfinity;
        private bool    _hasLead;
        private bool    _hunting;   // it knows a hostile is about, not just that something seemed off
        private float   _cautiousUntil = float.NegativeInfinity;
        private float   _underFireAt   = float.NegativeInfinity;
        private HealthManager _lastAttacker;
        private float   _lastAttackedAt = float.NegativeInfinity;
        private readonly HashSet<EnemyAI> _bodiesSeen = new();

        // What a look at one candidate turned up.
        private struct Stimulus
        {
            public bool  Seen;
            public bool  Instant;
            public float Gain;
            public TargetTraits Traits;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Bodies.Clear();
            Brains.Clear();
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
            _weights           = data.TargetPriorities != null ? data.TargetPriorities.Weights : TargetWeights.Default;
            _retaliationMemory = data.TargetPriorities != null ? data.TargetPriorities.RetaliationMemory : 0f;
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
        // Seconds since it last actually sensed its target (infinity if never).
        public float TimeSinceSensed => _clock - _lastSensedAt;

        // Where it believes the hostile (or the disturbance) is: what it last sensed, or its guess.
        public Vector3 LastKnownPosition { get; private set; }
        // Where the last thing it noticed came from — what it turns to look at.
        public Vector3 LookPosition      { get; private set; }
        // Which way a lost target seemed to be heading (zero if unknown) — searches lean that way.
        public Vector3 SearchDirection   { get; private set; }
        // Where to aim or measure from: the target itself while sensed, otherwise the belief.
        public Vector3 TargetPosition    => IsSensing ? Target.transform.position : LastKnownPosition;
        // Which way and how fast (flat, m/s) it saw its target moving — zero unless sensed right
        // now. Lets slow projectiles be aimed where the target is going.
        public Vector3 TargetVelocity    => IsSensing ? _sighting.Velocity : Vector3.zero;
        // Which way it last saw its target facing (flat unit vector; zero if never seen) — what
        // the target was watching, so what to stay out of when flanking.
        public Vector3 TargetFacing      => Target != null ? _sighting.Facing : Vector3.zero;
        // How it last saw its target moving relative to that facing.
        public TargetMotion TargetMotion => Target != null ? _sighting.Motion : TargetMotion.Unknown;
        // Its target's health and shield left (0–1) when it last saw it — 1 if never seen.
        public float TargetHealthShare { get; private set; } = 1f;
        // Whether its target was fighting someone else when it last saw it.
        public bool  IsTargetDistracted { get; private set; }

        // A lead (something new to look into) not yet consumed — read without consuming it.
        public bool HasLead => _hasLead;

        // Whether any hostile was within sensing range at the last sample, noticed or not.
        // Only for deciding how often the enemy thinks — never for what it knows.
        public bool SensesHostiles => _candidates.Count > 0;

        // On edge after a search that found nothing: quicker to notice, slower to settle.
        public bool IsCautious => _clock < _cautiousUntil;

        // Shot at moments ago — hit, a bullet cracking past, or a target it sees aiming at it.
        public bool    IsUnderFire     => _clock - _underFireAt <= UnderFireMemory;
        // Roughly where the fire came from (where to dodge away from).
        public Vector3 ThreatDirection { get; private set; }

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

        // A search turned up nothing: it forgets the target but stays on edge for a while.
        public void GiveUp()
        {
            LoseTarget();
            _cautiousUntil = _clock + _profile.CautionDuration;
        }

        // Back to how it was spawned: calm, knowing nothing (respawn, pooling).
        public void Reset()
        {
            LoseTarget();
            _cautiousUntil = float.NegativeInfinity;
            _bodiesSeen.Clear();
            _candidates.Clear();
            _lastAttacker   = null;
            _lastAttackedAt = float.NegativeInfinity;
        }

        // Calms down completely and forgets its target.
        public void LoseTarget()
        {
            Target   = null;
            _hunting = false;
            _hasLead = false;
            _lastSensedAt = float.NegativeInfinity;
            SearchDirection = Vector3.zero;
            _sighting.Forget();
            TargetHealthShare  = 1f;
            IsTargetDistracted = false;
            _meter.Clear();
            Level = AwarenessLevel.Unaware;
        }

        // --- Pushed stimuli ---------------------------------------------------------

        public void OnNoise(NoiseEvent noise)
        {
            if (noise.Source.Owner == _self.gameObject || IsTargetDetected) return;

            float distance = Vector3.Distance(noise.Position, _self.position);
            if (!CanHear(noise.Position, distance, noise.Radius, noise.Source.Owner != null ? noise.Source.Owner.transform : null)) return;

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
            if (!IsHostile(hit.Source) || hit.Source.Owner == null) return;

            Vector3 attacker = hit.Source.Owner.transform.position;
            float   distance = Vector3.Distance(attacker, _self.position);
            FeelFire(attacker + Error(distance, _profile.HitError));
            // Remembered even mid-fight with someone else: whom to shoot back at (see Sample).
            _lastAttacker   = hit.Source.Owner.GetComponentInParent<HealthManager>();
            _lastAttackedAt = _clock;
            if (IsTargetDetected) return;

            _meter.Raise(_profile.HitAwareness, 0.95f);
            _hunting = true;
            if (Target == null) Target = _lastAttacker;
            Lead(attacker + Error(distance, _profile.HitError));
            UpdateLevel();
        }

        // A hostile bullet landing close by: it knows it's being shot at, and roughly from where.
        public void OnImpact(Impact impact)
        {
            if (impact.Kind != ImpactKind.Bullet || !IsHostile(impact.Source) || impact.Source.Owner == null) return;
            if ((impact.Point - _self.position).sqrMagnitude > NearMissRadius * NearMissRadius) return;

            Vector3 shooter  = impact.Source.Owner.transform.position;
            float   distance = Vector3.Distance(shooter, _self.position);
            FeelFire(shooter + Error(distance, _profile.HitError));
            if (IsTargetDetected) return;

            _meter.Raise(_profile.NoiseAwareness, _profile.HearingCeiling);
            _hunting = true;
            if (_meter.IsSuspicious) Lead(shooter + Error(distance, _profile.HitError));
            UpdateLevel();
        }

        private void FeelFire(Vector3 from)
        {
            _underFireAt = _clock;
            Vector3 direction = from - _self.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f) ThreatDirection = direction.normalized;
        }

        public void OnCallout(EnemyCallout callout)
        {
            if (callout.Caller == _owner || callout.Team != _team || IsTargetDetected) return;

            float distance = Vector3.Distance(callout.From, _self.position);
            bool heard = callout.ThroughWalls ? distance <= callout.Radius : CanHear(callout.From, distance, callout.Radius, callout.Caller != null ? callout.Caller.transform : null);
            if (!heard) return;

            _meter.RaiseToSuspicious(0.1f);
            _hunting = true;
            float reportedDistance = Vector3.Distance(callout.From, callout.Reported);
            Lead(callout.Reported + Error(reportedDistance, _profile.HearingError));
            UpdateLevel();
        }

        // An ally died; heard only if close — anything further it has to find (see NoticeFallen).
        public void OnAllyFell(FallenAlly fallen)
        {
            if (fallen.Body == _owner || fallen.Team != _team || _bodiesSeen.Contains(fallen.Body)) return;

            float distance = Vector3.Distance(fallen.Position, _self.position);
            if (!CanHear(fallen.Position, distance, DeathHearingRange, fallen.Body.transform)) return;
            DiscoverBody(fallen);
        }

        // A vague reason to go and look (the director's patrol probes): mildly curious, not
        // hunting, and only when it has nothing else on its mind.
        public bool Nudge(Vector3 position)
        {
            if (IsTargetDetected || _hasLead || Level != AwarenessLevel.Unaware) return false;

            _meter.RaiseToSuspicious(0.05f);
            Lead(position);
            UpdateLevel();
            return true;
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
                stimulus.Traits = TraitsOf(candidate);

                float score = stimulus.Instant ? float.MaxValue : stimulus.Gain * (stimulus.Seen ? 2f : 1f) * _weights.Weigh(stimulus.Traits);
                if (score <= bestScore) continue;

                best = candidate;
                bestStimulus = stimulus;
                bestScore = score;
            }

            NoticeFallen();

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

        private TargetTraits TraitsOf(HealthManager candidate) => new(
            isCurrent:      candidate == Target,
            hurtItRecently: candidate == _lastAttacker && _clock - _lastAttackedAt <= _retaliationMemory,
            isDistracted:   IsFightingSomeoneElse(candidate),
            healthShare:    StrengthLeft(candidate));

        // An enemy engaged with a target other than this one — its attention is elsewhere.
        // The player's attention can't be read, so the player never counts as distracted.
        private bool IsFightingSomeoneElse(HealthManager candidate)
        {
            if (!Brains.TryGetValue(candidate, out EnemyAI brain))
            {
                candidate.TryGetComponent(out brain);
                Brains[candidate] = brain;
            }
            if (brain == null || brain.Perception == null) return false;

            EnemyPerception other = brain.Perception;
            return other.IsTargetDetected && other.Target != null && other.Target != _owner.Health;
        }

        private static float StrengthLeft(HealthManager candidate)
        {
            float full = candidate.MaxHealth + candidate.MaxShield;
            return full > 0f ? Mathf.Clamp01((candidate.Health + candidate.Shield) / full) : 1f;
        }

        private void Observe(HealthManager target, Stimulus stimulus, float elapsed)
        {
            Vector3 position = target.transform.position;
            if (target != Target)
            {
                Target = target;
                _sighting.Forget();
            }

            _sighting.Record(position, target.transform.forward, _clock);
            _lastSensedAt      = _clock;
            TargetHealthShare  = stimulus.Traits.HealthShare;
            IsTargetDistracted = stimulus.Traits.IsDistracted;

            // Seeing it face this way squarely reads as being aimed at.
            Vector3 toSelf = _self.position - position;
            toSelf.y = 0f;
            if (toSelf.sqrMagnitude > 0.01f && Vector3.Angle(_sighting.Facing, toSelf) <= AimedAtAngle && _meter.IsSuspicious)
                FeelFire(position);
            LastKnownPosition = position;
            LookPosition      = position;

            if (stimulus.Instant) _meter.Fill();
            else _meter.Raise(stimulus.Gain * elapsed * SightBonus());

            // A glimpse it isn't sure of yet: it wants a closer look.
            if (!_meter.IsFull && _meter.IsSuspicious) _hasLead = true;
        }

        private float SightBonus()
        {
            float bonus = _meter.IsSuspicious ? _profile.SearchingSightBonus : 1f;
            return IsCautious ? bonus * _profile.CautionSightBonus : bonus;
        }

        // Bodies of allies it hasn't found yet, in view with a clear line: something is badly wrong.
        private void NoticeFallen()
        {
            IReadOnlyList<FallenAlly> bodies = FallenAllies.All;
            if (bodies.Count == 0) return;

            Vector3 eye       = _self.position + Vector3.up * EyeHeight;
            float   halfAngle = _data.SightAngle * 0.5f;
            int     checks    = 0;

            for (int i = 0; i < bodies.Count && checks < MaxBodyChecks; i++)
            {
                FallenAlly fallen = bodies[i];
                if (fallen.Body == null || fallen.Body == _owner || fallen.Team != _team || _bodiesSeen.Contains(fallen.Body)) continue;

                Vector3 body = fallen.Position + Vector3.up * 0.3f;
                Vector3 toBody = body - eye;
                if (toBody.sqrMagnitude > _data.SightRange * _data.SightRange) continue;
                if (Vector3.Angle(_self.forward, toBody) > halfAngle) continue;

                checks++;
                if (IsClear(eye, body, fallen.Body.transform)) DiscoverBody(fallen);
            }
        }

        private void DiscoverBody(FallenAlly fallen)
        {
            _bodiesSeen.Add(fallen.Body);
            _owner.Morale.OnAllyDown();
            if (IsTargetDetected) return;

            _meter.RaiseToSuspicious(0.15f);
            _hunting = true;
            Lead(fallen.Position);
            UpdateLevel();
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

        // Out of sight: guess where it went from how it was moving, and go looking.
        private void OnTargetLost()
        {
            LastKnownPosition = _sighting.Predict(_profile.Prediction.EvaluateClamped(),
                                                  _profile.StrafePrediction, _profile.BackpedalPrediction);
            SearchDirection   = _sighting.Heading;
            LookPosition      = _sighting.Position;
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

            if (!IsClear(eye, chest, target.transform) && !IsClear(eye, head, target.transform)) return false;

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
            if (distance > range || !CanHear(target.transform.position, distance, range, target.transform)) return 0f;

            return _profile.FootstepGain * (1f - 0.5f * distance / range);
        }

        // Within range, and still within it once a wall in between has muffled it. `emitter` is
        // the character making the sound, if any — its own body doesn't muffle it.
        private bool CanHear(Vector3 source, float distance, float range, Transform emitter = null)
        {
            if (distance > range) return false;
            if (distance <= range * _profile.OccludedHearing) return true;

            Vector3 ears = _self.position + Vector3.up * EyeHeight;
            return IsClear(ears, source + Vector3.up * 0.5f, emitter);
        }

        // `subject` is the character (or body) the line ends on, which never blocks its own view.
        private bool IsClear(Vector3 from, Vector3 to, Transform subject) =>
            !ConcealmentZone.BlocksLine(from, to) && SightLines.IsClear(from, to, _obstacleMask, subject);

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
