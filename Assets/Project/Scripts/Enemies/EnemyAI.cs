using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using CGD.Abilities;
using CGD.Audio;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Core;
using CGD.Impacts;
using CGD.Items;
using CGD.Stealth;
using CGD.Stats;

namespace CGD.Enemies
{
    // An enemy's body and senses — movement, perception, melee and ranged attacks — plus
    // the behavior tree (EnemyData.BehaviorTree) that decides what to do with them. Stuns
    // and death are handled here, outside the tree.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Stunnable))]
    public class EnemyAI : MonoBehaviour, IPoolable, IBehaviorTreeOwner, IBtAgentStatus
    {
        // Investigate: curious, checking something out. Hunt: knows a hostile is about and is
        // searching for it. Return: heading back to its post after giving up (or a kill).
        // Tree nodes report which of these they are doing, for visuals, animation and audio.
        public enum AiState { Patrol, Investigate, Hunt, Return, Chase, Retreat, RaiseAlarm, Stunned, Dead }

        private const float TurnSpeed = 540f;
        // Alarm panels within this distance of its post are ones it knows about.
        private const float AlarmKnownRadius = 25f;
        private const float CorneredDistance = 3f;

        [SerializeField] private EnemyData   _data;
        [SerializeField] private Transform[] _waypoints;
        [Tooltip("Layers hostile characters can be found on")]
        [SerializeField] private LayerMask   _targetMask = ~0;
        [Tooltip("Geometry that blocks line of sight")]
        [SerializeField] private LayerMask   _obstacleMask;

        private static readonly Collider[] _attackBuffer = new Collider[16];
        private static readonly ProfilerMarker SensesMarker = new("EnemyAI.Senses");
        private readonly HashSet<HealthManager> _attackHits = new();

        private EnemyHealth  _health;
        private Stunnable    _stunnable;
        private DamageSource _damageSource;
        private CharacterStats _stats;
        private Stealthable  _stealth;

        // The brain: the tree, the state its nodes last reported (for visuals), and time
        // gathered since its last tick (calm enemies think less often, see TreeTickRate).
        private BehaviorTree _tree;
        private AiState      _state = AiState.Patrol;
        private AiState      _stateBeforeStun;
        private float        _sinceTreeTick;
        private bool         _started;

        // Turning toward a point, spread over the frames until the tree's next tick.
        private Vector3 _facePoint;
        private float   _faceTimeLeft;

        private CooldownTimer _attackCooldown;
        private float _windupTimer;
        private float _nextStaggerAt;
        private bool  _alarmTried;
        private EngageStep    _step;
        private HealthManager _engagedTarget;

        public event Action<AiState> StateChanged;
        // Melee wind-up begins (the hit lands AttackWindup seconds later).
        public event Action AttackStarted;
        // A ranged shot leaves the enemy.
        public event Action Fired;
        // What it is doing within a fight changed (for animation and audio).
        public event Action<EngageStep> StepChanged;
        // A heavy hit staggered it.
        public event Action Staggered;
        // A grenade left its hand.
        public event Action Threw;
        // Vanished from this position and reappeared where it now stands (Phase Shift).
        public event Action<Vector3> Relocated;
        // An ability it cast went off (after its wind-up).
        public event Action<Ability> AbilityUsed;

        public AiState State => _state;

        // The running tree (null when its EnemyData has none), for the tree editor's live view.
        public BehaviorTree Tree => _tree;

        // What it believes is going on (for the awareness indicator and stealth feedback).
        public AwarenessLevel Awareness      => Perception.Level;
        public float          AwarenessValue => Perception.Awareness;
        public bool           IsCautious     => Perception.IsCautious;
        public EngageStep     Step           => _step;
        public float          MoraleValue    => Morale.Value;

        internal NavMeshAgent      Agent        { get; private set; }
        internal EnemyPerception   Perception   { get; private set; }
        internal EnemyRangedCombat RangedCombat { get; private set; }
        internal EnemySearch       Search       { get; private set; }
        internal EnemyTactics      Tactics      { get; private set; }
        internal EnemyPost         Post         { get; private set; }
        internal EnemyAbilities    Abilities    { get; private set; }
        internal EnemyAmbush       Ambush       { get; private set; }
        internal EnemyMinions      Minions      { get; private set; }
        internal EnemyBelief       Belief       { get; private set; }
        internal GrenadeThrower    Grenades     { get; private set; }
        internal EnemySquad        Squad        { get; private set; }
        internal SquadRole         SquadRole    => Squad != null ? Squad.RoleOf(this) : SquadRole.None;
        internal EnemyMorale       Morale       { get; private set; }
        internal EnemyVoice        Voice        { get; private set; }

        // Set when it gave up a search or won its fight: it should head back to its post.
        // Shaken and not cornered: time to fall back. Cornered, it fights instead.
        internal bool ShouldRetreat => Morale.IsShaken && !IsCornered;
        internal bool IsCornered =>
            Perception.IsTargetDetected &&
            (Perception.TargetPosition - transform.position).sqrMagnitude < CorneredDistance * CorneredDistance;

        internal bool    ReturnPending   { get; private set; }
        internal bool    ReturnIsVictory { get; private set; }
        internal Vector3 ReturnLookAt    { get; private set; }

        // Stuck at the end of a partial path: the target is somewhere it can't walk to.
        internal bool IsPathBlocked =>
            Agent.isOnNavMesh && !Agent.pathPending && Agent.pathStatus == NavMeshPathStatus.PathPartial &&
            Agent.remainingDistance < 0.6f;
        internal EnemyHealth       Health       => _health;
        internal EnemyData         Data         => _data;
        internal Transform[]       Waypoints    => _waypoints ?? Array.Empty<Transform>();
        internal bool              IsAttacking  { get; private set; }
        internal bool              CanAttack    => !IsAttacking && _attackCooldown.IsReady;
        internal bool              IsStunned    => _stunnable.IsStunned;

        private void Awake()
        {
            Agent         = GetComponent<NavMeshAgent>();
            _health       = GetComponent<EnemyHealth>();
            _stunnable    = GetComponent<Stunnable>();
            _damageSource = DamageSource.Of(gameObject);
            TryGetComponent(out _stats);
            Perception    = new EnemyPerception(this, _data, _targetMask, _obstacleMask, _health.Team);
            RangedCombat  = new EnemyRangedCombat(this, _damageSource, _obstacleMask);
            Search        = new EnemySearch(this);
            Tactics       = new EnemyTactics(this, _obstacleMask);
            Post          = new EnemyPost(this);
            TryGetComponent(out _stealth);
            Ambush        = new EnemyAmbush(_stealth);
            Minions       = new EnemyMinions(this);
            Belief        = new EnemyBelief(this, _obstacleMask);
            Abilities     = new EnemyAbilities(this, _damageSource, _stealth, Minions);
            Grenades      = new GrenadeThrower(this, _damageSource, _obstacleMask);
            Morale        = new EnemyMorale(this);
            Voice         = new EnemyVoice(this);
            _health.OnDeath += OnDeath;
        }

        // The tree is built here rather than in Awake: rolled affixes swap in their data
        // (UseData) after Awake. Killed before its first frame, the enemy stays dead. Without
        // a tree it stands where it is (only its senses run).
        private void Start()
        {
            _started = true;
            Post.Remember();
            if (_data.BehaviorTree != null) _tree = _data.BehaviorTree.CreateInstance(gameObject, this);
            // Spread first ticks so a level's worth of calm enemies don't all think on one frame.
            _sinceTreeTick = UnityEngine.Random.value * TreeTickRate.Remote;
            if (_health.IsDead) ReportState(AiState.Dead);
        }

        private void OnEnable()
        {
            EnemyRoster.Add(this);
            Noise.Emitted         += Perception.OnNoise;
            EnemyCallouts.Raised  += Perception.OnCallout;
            FallenAllies.Fell     += Perception.OnAllyFell;
            ImpactEvents.Occurred += Perception.OnImpact;
            _health.OnHit         += Perception.OnHit;
            _health.OnDamaged     += OnDamaged;
        }

        private void OnDisable()
        {
            EnemyRoster.Remove(this);
            Noise.Emitted         -= Perception.OnNoise;
            EnemyCallouts.Raised  -= Perception.OnCallout;
            FallenAllies.Fell     -= Perception.OnAllyFell;
            ImpactEvents.Occurred -= Perception.OnImpact;
            _health.OnHit         -= Perception.OnHit;
            _health.OnDamaged     -= OnDamaged;

            // Switched off on death too (the body stays a body). A despawned or switched-off one
            // is gone: it lets go of its cover and anything it held in the fight.
            if (gameObject.activeInHierarchy) return;
            FallenAllies.Remove(this);
            Tactics.ReleaseCover();
            Tactics.EndMeleeTurn();
            AttackTokens.ReleaseAll(this);
            BoldMovePermits.Release(this);
        }

        private void OnDestroy()
        {
            _health.OnDeath -= OnDeath;
            FallenAllies.Remove(this);
            AttackTokens.ReleaseAll(this);
            BoldMovePermits.Release(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Minions.Tick(Time.time);

            // A stun freezes attacks and senses as well as movement.
            if (!_stunnable.IsStunned)
            {
                using (SensesMarker.Auto())
                {
                    _attackCooldown.Tick(dt);
                    TickAttack(dt);
                    RangedCombat.Tick(dt);
                    Perception.Tick(dt);
                    Belief.Tick(Time.time);
                    Tactics.Tick(Time.time);
                    Morale.Tick(dt);
                    TrackEngagement();
                }
            }

            if (_tree != null) TickTree(dt);
            TickFacing(dt);
        }

        // A stun pauses the tree where it is, so the enemy resumes what it was doing.
        private void TickTree(float dt)
        {
            if (_stunnable.IsStunned)
            {
                if (_state != AiState.Stunned) _stateBeforeStun = _state;
                Agent.isStopped = true;
                ReportState(AiState.Stunned);
                return;
            }

            if (_state == AiState.Stunned)
            {
                Agent.isStopped = false;
                ReportState(_stateBeforeStun);
            }

            _sinceTreeTick += dt;
            if (_sinceTreeTick < TreeTickInterval()) return;

            EnemyBlackboard.Publish(this, _tree.Blackboard);
            _tree.Tick(_sinceTreeTick);
            _sinceTreeTick = 0f;
        }

        // Every frame once anything has its attention; the moment its senses pick something
        // up the interval drops to zero, so a calm enemy reacts no later than a busy one.
        internal float TreeTickInterval()
        {
            bool engaged = Perception.Level != AwarenessLevel.Unaware || Perception.HasLead ||
                           Perception.IsUnderFire || IsAttacking || RangedCombat.IsBursting;
            return TreeTickRate.IntervalFor(engaged, Perception.SensesHostiles);
        }

        void IBtAgentStatus.DescribeStatus(List<(string Label, string Value)> rows) => EnemyStatusReport.Describe(this, rows);

        // For enemies spawned at runtime (level builder, dev console) instead of placed with a route.
        public void SetWaypoints(Transform[] waypoints) => _waypoints = waypoints;

        // Says something from its type's bark set, if the level isn't too noisy (the director's
        // narration, scripted moments). False when nothing was said.
        public bool Bark(BarkKind kind) => Voice.Say(kind);

        // Sends a calm enemy to have a look round an area (the director's patrol probes). False if
        // it already has something on its mind.
        public bool CheckArea(Vector3 area) => Perception.Nudge(area);

        // Sends the enemy to investigate a position, e.g. a reinforcement sent toward the player.
        public void AlertTo(Vector3 position, DamageSource source) => Perception.Alert(position, source);

        // Rolled affixes run the enemy on a modified copy of its data (perception keeps the original).
        public void UseData(EnemyData data) => _data = data;

        // A search found nothing: stay on edge and head back to its post.
        internal void GiveUpSearch()
        {
            Perception.GiveUp();
            BeginReturn(victory: false, transform.position + transform.forward);
        }

        internal void FinishReturn() => ReturnPending = false;

        internal void ReportStep(EngageStep step)
        {
            if (_step == step) return;
            _step = step;
            StepChanged?.Invoke(step);
        }

        // A panel near its post it can still raise, if this enemy raises alarms and hasn't tried yet.
        internal bool TryFindAlarm(out AlarmPanel panel)
        {
            panel = null;
            return !_alarmTried && TryFindAlarmIgnoringTried(out panel);
        }

        internal bool TryFindAlarmIgnoringTried(out AlarmPanel panel)
        {
            panel = null;
            if (!_data.RaisesAlarm) return false;

            float best = float.MaxValue;
            foreach (AlarmPanel candidate in AlarmPanel.All)
            {
                if (!candidate.IsAvailable || candidate.Team != _health.Team) continue;
                if (!Post.IsNear(candidate.transform.position, AlarmKnownRadius)) continue;

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; panel = candidate; }
            }
            return panel != null;
        }

        internal void MarkAlarmTried() => _alarmTried = true;

        // Behavior tree nodes say what the enemy is doing, so its visor, animation and
        // stealth feedback match (patrolling, investigating, fighting).
        internal void ReportState(AiState state)
        {
            if (state != AiState.Chase) ReportStep(EngageStep.None);
            if (_state == state) return;

            // Out of the fight: give the melee turn to an ally (a stun keeps it).
            if (_state == AiState.Chase && state != AiState.Stunned) Tactics.EndMeleeTurn();
            _state = state;
            StateChanged?.Invoke(state);
        }

        // What blocks sight and shots, for nodes that need to know whether a view is open.
        internal LayerMask ObstacleMask => _obstacleMask;

        // Slowed by status effects, and sped up or slowed by buffs (MovementSpeed stat, base 1).
        internal void SetSpeed(float speed) => Agent.speed = speed * _stunnable.SpeedMultiplier * SpeedFromStats;

        // Buffs speed up the melee swing (FireRate stat, base 1): wind-up and recovery both shrink.
        private float AttackSpeedFromStats => _stats != null ? Mathf.Max(0.1f, _stats.Apply(ItemStat.FireRate, 1f)) : 1f;

        private float SpeedFromStats => _stats != null ? Mathf.Max(0.1f, _stats.Apply(ItemStat.MovementSpeed, 1f)) : 1f;

        // Turns as far as deltaTime allows. The turn itself is applied frame by frame (TickFacing),
        // so it stays smooth when a calm enemy's tree ticks only a few times a second.
        internal void FaceTowards(Vector3 position, float deltaTime)
        {
            _facePoint    = position;
            _faceTimeLeft = deltaTime;
        }

        private void TickFacing(float dt)
        {
            if (_faceTimeLeft <= 0f) return;
            float step = Mathf.Min(dt, _faceTimeLeft);
            _faceTimeLeft -= step;

            Vector3 flat = _facePoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(flat), TurnSpeed * step);
        }

        internal void TryStartAttack()
        {
            if (!CanAttack) return;

            IsAttacking  = true;
            float swing = 1f / AttackSpeedFromStats;
            _windupTimer = _data.AttackWindup * swing;
            _attackCooldown.Start((_data.AttackWindup + _data.AttackCooldown) * swing);
            AttackStarted?.Invoke();
        }

        internal void NotifyFired()
        {
            Reveal();
            Fired?.Invoke();
        }

        internal void NotifyThrew()
        {
            Reveal();
            Threw?.Invoke();
        }

        // Attacking and casting give away an enemy hiding in cover (or cloaked).
        internal void Reveal() => _stealth?.Reveal();

        internal void NotifyRelocated(Vector3 from) => Relocated?.Invoke(from);

        internal void NotifyAbilityUsed(Ability ability) => AbilityUsed?.Invoke(ability);

        internal void JoinSquad(EnemySquad squad) => Squad = squad;

        private void TickAttack(float dt)
        {
            if (!IsAttacking) return;

            _windupTimer -= dt;
            if (_windupTimer > 0f) return;

            IsAttacking = false;
            ResolveAttack();
            Tactics.OnStrike();
        }

        private void ResolveAttack()
        {
            _data.AttackSound.TryPlay(transform.position);

            float   range  = _data.AttackRange;
            Vector3 center = transform.position + Vector3.up + transform.forward * (range * 0.5f);
            int count = Physics.OverlapSphereNonAlloc(center, range * 0.6f, _attackBuffer,
                _targetMask, QueryTriggerInteraction.Ignore);

            _attackHits.Clear();
            float damage = _stats != null ? _stats.Apply(ItemStat.Damage, _data.AttackDamage) : _data.AttackDamage;
            damage *= Ambush.Consume();
            Reveal();
            var info = new DamageInfo(damage, source: _damageSource);

            for (int i = 0; i < count; i++)
            {
                if (Hitbox.FindDamageable(_attackBuffer[i]) is not HealthManager target) continue;
                if (target == _health || !_attackHits.Add(target)) continue;
                target.TakeDamage(info);
            }
        }

        // Remembers who it is fighting so it notices when that fight is won.
        private void TrackEngagement()
        {
            if (Perception.IsTargetDetected)
            {
                _engagedTarget = Perception.Target;
            }
            else if (_engagedTarget != null && _engagedTarget.IsDead)
            {
                BeginReturn(victory: true, _engagedTarget.transform.position);
                _engagedTarget = null;
            }
            else if (Perception.Level == AwarenessLevel.Unaware)
            {
                _engagedTarget = null;
            }
        }

        private void BeginReturn(bool victory, Vector3 lookAt)
        {
            ReturnPending   = true;
            ReturnIsVictory = victory;
            ReturnLookAt    = lookAt;
            _alarmTried     = false;
        }

        // A heavy hit knocks it off balance: a short stun that also spoils a wind-up.
        private void OnDamaged(float amount)
        {
            Morale.OnDamaged();
            if (_health.IsDead || Time.time < _nextStaggerAt) return;
            if (amount < _health.MaxHealth * _data.StaggerThreshold) return;

            _nextStaggerAt = Time.time + _data.StaggerCooldown;
            IsAttacking = false;
            RangedCombat.CancelBurst();
            Abilities.Cancel();
            Grenades.Cancel();
            _stunnable.ApplyStun(_data.StaggerDuration);
            Staggered?.Invoke();
        }

        private void OnDeath()
        {
            Minions.DismissAll();
            Belief.Clear();
            RangedCombat.CancelBurst();
            Search.Cancel();
            Tactics.ReleaseCover();
            Tactics.EndMeleeTurn();
            AttackTokens.ReleaseAll(this);
            BoldMovePermits.Release(this);
            FallenAllies.Add(new FallenAlly(this, transform.position, _health.Team));
            _tree?.Abort();
            ReportState(AiState.Dead);
            _faceTimeLeft = 0f;
            Agent.enabled = false;
            enabled = false;
        }

        // Reused from PrefabPool: wake the agent back up at the spawn position and start its
        // tree over. The first spawn is handled by Start.
        public void OnSpawned()
        {
            if (!_started) return;

            Agent.enabled = true;
            Agent.Warp(transform.position);
            enabled      = true;
            IsAttacking  = false;
            _attackCooldown.Reset();
            RangedCombat.Reset();
            Ambush.Disarm();
            Minions.DismissAll();
            Search.Cancel();
            Tactics.Reset();
            Perception.Reset();
            Belief.Clear();
            Morale.Reset();
            Voice.Reset();
            Abilities.Reset();
            Post.Remember();
            FallenAllies.Remove(this);
            ReturnPending  = false;
            _alarmTried    = false;
            _engagedTarget = null;
            _sinceTreeTick = 0f;

            _tree?.Reset();
            ReportState(AiState.Patrol);
        }
    }
}
