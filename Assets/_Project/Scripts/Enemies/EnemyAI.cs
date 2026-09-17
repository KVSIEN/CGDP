using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Stunnable))]
    public class EnemyAI : MonoBehaviour
    {
        public enum AiState { Patrol, Alert, Chase }

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
        private Dictionary<AiState, EnemyState> _states;
        private EnemyState   _current;

        private CooldownTimer _attackCooldown;
        private float _windupTimer;
        private bool  _wasStunned;

        public event Action<AiState> StateChanged;

        public AiState State => _current != null ? _current.Id : AiState.Patrol;

        internal NavMeshAgent    Agent      { get; private set; }
        internal EnemyPerception Perception { get; private set; }
        internal EnemyData       Data       => _data;
        internal Transform[]     Waypoints  => _waypoints ?? Array.Empty<Transform>();
        internal bool            IsAttacking { get; private set; }

        private void Awake()
        {
            Agent         = GetComponent<NavMeshAgent>();
            _health       = GetComponent<EnemyHealth>();
            _stunnable    = GetComponent<Stunnable>();
            _damageSource = DamageSource.Of(gameObject);
            Perception    = new EnemyPerception(transform, _data, _targetMask, _obstacleMask, _health.Team);

            EnemyState chaseState = _data.CombatType == EnemyCombatType.Ranged
                ? new RangedChaseState(this, _damageSource, _obstacleMask)
                : new ChaseState(this);

            _states = new Dictionary<AiState, EnemyState>
            {
                [AiState.Patrol] = new PatrolState(this),
                [AiState.Alert]  = new AlertState(this),
                [AiState.Chase]  = chaseState,
            };

            _health.OnDeath += OnDeath;
        }

        private void Start() => ChangeState(AiState.Patrol);

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
            if (_stunnable.IsStunned)
            {
                Agent.isStopped = true;
                _wasStunned = true;
                return;
            }

            if (_wasStunned)
            {
                Agent.isStopped = false;
                _wasStunned = false;
            }

            float dt = Time.deltaTime;
            _attackCooldown.Tick(dt);
            TickAttack(dt);

            Perception.Tick();
            _current.Tick(dt);
        }

        internal void ChangeState(AiState state)
        {
            _current?.Exit();
            _current = _states[state];
            _current.Enter();
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

        // -- Melee Attack ---------------------------------------------------------

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
            var info = new DamageInfo(_data.AttackDamage, source: _damageSource);

            for (int i = 0; i < count; i++)
            {
                if (Hitbox.FindDamageable(_attackBuffer[i]) is not HealthManager target) continue;
                if (target == _health || !_attackHits.Add(target)) continue;
                target.TakeDamage(info);
            }
        }

        private void OnDeath()
        {
            Agent.enabled = false;
            enabled = false;
        }
    }
}
