using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Audio;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Core;
using CGD.Items;
using CGD.Stats;

namespace CGD.Enemies
{
    // An enemy's body and senses — movement, perception, melee and ranged attacks — plus
    // the "brain" that decides what to do with them. The brain is either the built-in state
    // machine (Patrol, Alert, Chase) or, when EnemyData has one, a behavior tree. Stuns and
    // death are handled here for both.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Stunnable))]
    public class EnemyAI : MonoBehaviour, IPoolable, IBehaviorTreeOwner
    {
        public enum AiState { Patrol, Alert, Chase, Stunned, Dead }

        private const float TurnSpeed = 540f;

        [SerializeField] private EnemyData   _data;
        [SerializeField] private Transform[] _waypoints;
        [Tooltip("Layers hostile characters can be found on")]
        [SerializeField] private LayerMask   _targetMask = ~0;
        [Tooltip("Geometry that blocks line of sight")]
        [SerializeField] private LayerMask   _obstacleMask;

        private static readonly Collider[] _attackBuffer = new Collider[16];
        private readonly HashSet<HealthManager> _attackHits = new();

        private EnemyHealth  _health;
        private Stunnable    _stunnable;
        private DamageSource _damageSource;
        private CharacterStats _stats;
        private readonly StateMachine<AiState> _machine = new();

        // Behavior tree brain: the tree, and the state its nodes last reported (for visuals).
        private BehaviorTree _tree;
        private AiState      _treeState = AiState.Patrol;
        private AiState      _stateBeforeStun;
        private bool         _started;

        private CooldownTimer _attackCooldown;
        private float _windupTimer;

        public event Action<AiState> StateChanged;
        // Melee wind-up begins (the hit lands AttackWindup seconds later).
        public event Action AttackStarted;
        // A ranged shot leaves the enemy.
        public event Action Fired;

        public AiState State => _tree != null ? _treeState
                              : _machine.IsRunning ? _machine.Current : AiState.Patrol;

        // The running tree (null when the state machine decides), for the tree editor's live view.
        public BehaviorTree Tree => _tree;

        // What it believes is going on (for the awareness indicator and stealth feedback).
        public AwarenessLevel Awareness      => Perception.Level;
        public float          AwarenessValue => Perception.Awareness;

        internal NavMeshAgent      Agent        { get; private set; }
        internal EnemyPerception   Perception   { get; private set; }
        internal EnemyRangedCombat RangedCombat { get; private set; }
        internal EnemySearch       Search       { get; private set; }
        internal EnemyTactics      Tactics      { get; private set; }
        internal EnemyHealth       Health       => _health;
        internal EnemyData         Data         => _data;
        internal Transform[]       Waypoints    => _waypoints ?? Array.Empty<Transform>();
        internal bool              IsAttacking  { get; private set; }
        internal bool              CanAttack    => !IsAttacking && _attackCooldown.IsReady;
        internal bool              IsStunned    => _stunnable.IsStunned;
        internal AiState           PreviousState => _machine.Previous;

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

            EnemyState chaseState = _data.CombatType == EnemyCombatType.Ranged
                ? new RangedChaseState(this)
                : new ChaseState(this);

            // Patrol/Alert/Chase pick their own next state. Stun can interrupt any of them
            // and hands back to whatever was interrupted; Dead is final until respawn.
            _machine.Add(AiState.Patrol,  new PatrolState(this))
                    .Add(AiState.Alert,   new AlertState(this))
                    .Add(AiState.Chase,   chaseState)
                    .Add(AiState.Stunned, new StunnedState(this))
                    .Add(AiState.Dead)
                    .AddAnyTransition(AiState.Stunned, () => _stunnable.IsStunned)
                    .SetRule((from, to) => from != AiState.Dead);
            _machine.Changed += (_, next) => StateChanged?.Invoke(next);
            _health.OnDeath += OnDeath;
        }

        // The brain is chosen here rather than in Awake: rolled affixes swap in their data
        // (UseData) after Awake. Killed before its first frame, the enemy stays dead.
        private void Start()
        {
            _started = true;
            if (_data.BehaviorTree != null) _tree = _data.BehaviorTree.CreateInstance(gameObject, this);

            if (_tree != null)
            {
                if (_health.IsDead) ReportState(AiState.Dead);
            }
            else if (!_machine.IsRunning)
            {
                _machine.Start(AiState.Patrol);
            }
        }

        private void OnEnable()
        {
            Noise.Emitted         += Perception.OnNoise;
            EnemyCallouts.Raised  += Perception.OnCallout;
            _health.OnHit         += Perception.OnHit;
        }

        private void OnDisable()
        {
            Noise.Emitted         -= Perception.OnNoise;
            EnemyCallouts.Raised  -= Perception.OnCallout;
            _health.OnHit         -= Perception.OnHit;
        }

        private void OnDestroy() => _health.OnDeath -= OnDeath;

        private void Update()
        {
            float dt = Time.deltaTime;

            // A stun freezes attacks and senses as well as movement.
            if (!_stunnable.IsStunned)
            {
                _attackCooldown.Tick(dt);
                TickAttack(dt);
                RangedCombat.Tick(dt);
                Perception.Tick(dt);
                Tactics.Tick(Time.time);
            }

            if (_tree != null) TickTree(dt);
            else               _machine.Tick(dt);
        }

        // A stun pauses the tree where it is (like StunnedState), so the enemy resumes what it was doing.
        private void TickTree(float dt)
        {
            if (_stunnable.IsStunned)
            {
                if (_treeState != AiState.Stunned) _stateBeforeStun = _treeState;
                Agent.isStopped = true;
                ReportState(AiState.Stunned);
                return;
            }

            if (_treeState == AiState.Stunned)
            {
                Agent.isStopped = false;
                ReportState(_stateBeforeStun);
            }
            _tree.Tick(dt);
        }

        // For enemies spawned at runtime (level builder, dev console) instead of placed with a route.
        public void SetWaypoints(Transform[] waypoints) => _waypoints = waypoints;

        // Sends the enemy to investigate a position, e.g. a reinforcement sent toward the player.
        public void AlertTo(Vector3 position, DamageSource source) => Perception.Alert(position, source);

        // Rolled affixes run the enemy on a modified copy of its data (perception keeps the original).
        public void UseData(EnemyData data) => _data = data;

        internal void ChangeState(AiState state) => _machine.TryChangeTo(state);

        // Behavior tree nodes say what the enemy is doing, so its visor, animation and
        // stealth feedback match (patrolling, investigating, fighting).
        internal void ReportState(AiState state)
        {
            if (_treeState == state) return;
            _treeState = state;
            StateChanged?.Invoke(state);
        }

        internal void SetSpeed(float speed) => Agent.speed = speed * _stunnable.SpeedMultiplier;

        internal void FaceTowards(Vector3 position, float deltaTime)
        {
            Vector3 flat = position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(flat), TurnSpeed * deltaTime);
        }

        internal void TryStartAttack()
        {
            if (!CanAttack) return;

            IsAttacking  = true;
            _windupTimer = _data.AttackWindup;
            _attackCooldown.Start(_data.AttackWindup + _data.AttackCooldown);
            AttackStarted?.Invoke();
        }

        internal void NotifyFired() => Fired?.Invoke();

        private void TickAttack(float dt)
        {
            if (!IsAttacking) return;

            _windupTimer -= dt;
            if (_windupTimer > 0f) return;

            IsAttacking = false;
            ResolveAttack();
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
            var info = new DamageInfo(damage, source: _damageSource);

            for (int i = 0; i < count; i++)
            {
                if (Hitbox.FindDamageable(_attackBuffer[i]) is not HealthManager target) continue;
                if (target == _health || !_attackHits.Add(target)) continue;
                target.TakeDamage(info);
            }
        }

        private void OnDeath()
        {
            RangedCombat.CancelBurst();
            Search.Cancel();
            Tactics.ReleaseCover();
            if (_tree != null)
            {
                _tree.Abort();
                ReportState(AiState.Dead);
            }
            else
            {
                _machine.ForceChangeTo(AiState.Dead);
            }
            Agent.enabled = false;
            enabled = false;
        }

        // Reused from PrefabPool: wake the agent back up at the spawn position and start
        // over from Patrol. The first spawn is handled by Start.
        public void OnSpawned()
        {
            if (!_started) return;

            Agent.enabled = true;
            Agent.Warp(transform.position);
            enabled      = true;
            IsAttacking  = false;
            _attackCooldown.Reset();
            RangedCombat.Reset();
            Search.Cancel();
            Tactics.Reset();
            Perception.LoseTarget();

            if (_tree != null)
            {
                _tree.Reset();
                ReportState(AiState.Patrol);
            }
            else
            {
                _machine.ForceChangeTo(AiState.Patrol);
            }
        }
    }
}
