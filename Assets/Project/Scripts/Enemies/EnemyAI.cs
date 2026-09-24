using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Core;
using CGD.Items;
using CGD.Stats;

namespace CGD.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Stunnable))]
    public class EnemyAI : MonoBehaviour, IPoolable
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

        private CooldownTimer _attackCooldown;
        private float _windupTimer;

        public event Action<AiState> StateChanged;

        public AiState State => _machine.IsRunning ? _machine.Current : AiState.Patrol;

        internal NavMeshAgent    Agent      { get; private set; }
        internal EnemyPerception Perception { get; private set; }
        internal EnemyData       Data       => _data;
        internal Transform[]     Waypoints  => _waypoints ?? Array.Empty<Transform>();
        internal bool            IsAttacking { get; private set; }
        internal bool            IsStunned   => _stunnable.IsStunned;
        internal AiState         PreviousState => _machine.Previous;

        private void Awake()
        {
            Agent         = GetComponent<NavMeshAgent>();
            _health       = GetComponent<EnemyHealth>();
            _stunnable    = GetComponent<Stunnable>();
            _damageSource = DamageSource.Of(gameObject);
            TryGetComponent(out _stats);
            Perception    = new EnemyPerception(transform, _data, _targetMask, _obstacleMask, _health.Team);

            EnemyState chaseState = _data.CombatType == EnemyCombatType.Ranged
                ? new RangedChaseState(this, _damageSource, _obstacleMask)
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

        // Killed before its first frame, the machine is already running (in Dead).
        private void Start()
        {
            if (!_machine.IsRunning) _machine.Start(AiState.Patrol);
        }

        private void OnEnable()
        {
            Noise.Emitted += Perception.OnNoise;
            _health.OnHit += Perception.OnHit;
        }

        private void OnDisable()
        {
            Noise.Emitted -= Perception.OnNoise;
            _health.OnHit -= Perception.OnHit;
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
                Perception.Tick();
            }

            _machine.Tick(dt);
        }

        internal void ChangeState(AiState state) => _machine.TryChangeTo(state);

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
            if (IsAttacking || !_attackCooldown.IsReady) return;

            IsAttacking  = true;
            _windupTimer = _data.AttackWindup;
            _attackCooldown.Start(_data.AttackWindup + _data.AttackCooldown);
        }

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
            _data.AttackSound?.Play(transform.position);

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
            _machine.ForceChangeTo(AiState.Dead);
            Agent.enabled = false;
            enabled = false;
        }

        // Reused from PrefabPool: wake the agent back up at the spawn position and start
        // over from Patrol. The first spawn is handled by Start.
        public void OnSpawned()
        {
            if (!_machine.IsRunning) return;

            Agent.enabled = true;
            Agent.Warp(transform.position);
            enabled      = true;
            IsAttacking  = false;
            _attackCooldown.Reset();
            Perception.LoseTarget();
            _machine.ForceChangeTo(AiState.Patrol);
        }
    }
}
