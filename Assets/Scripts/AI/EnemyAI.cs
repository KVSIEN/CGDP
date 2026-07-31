using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour, IStunnable
{
    public enum AiState { Patrol, Alert, Chase }

    [SerializeField] private EnemyData   _data;
    [SerializeField] private Transform   _playerTransform;
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private LayerMask   _obstacleMask;
    [SerializeField] private Renderer[]  _stateRenderers;

    [Header("State Colors")]
    [SerializeField] private Color _patrolColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color _alertColor  = new Color(1f,   0.8f, 0f  );
    [SerializeField] private Color _chaseColor  = new Color(1f,   0.15f, 0.15f);

    private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    private NavMeshAgent          _agent;
    private EnemyHealth           _health;
    private BtNode                _tree;
    private MaterialPropertyBlock _mpb;

    private AiState _state = AiState.Patrol;
    private int     _waypointIndex;
    private Vector3 _lastKnownPos;
    private float   _alertTimer;
    private CooldownTimer _attackCooldown;
    private bool    _seesPlayer;
    private float   _speedMultiplier = 1f;
    private CooldownTimer _stunTimer;
    private bool    _wasStunned;

    private int   _burstRemaining;
    private float _burstTimer;
    private float _strafeTimer;
    private int   _strafeDir = 1;

    public float SpeedMultiplier
    {
        get => _speedMultiplier;
        set => _speedMultiplier = Mathf.Clamp01(value);
    }
    public bool IsStunned => !_stunTimer.IsReady;
    public void ApplyStun(float duration) => _stunTimer.Start(duration);

    private void Awake()
    {
        _agent  = GetComponent<NavMeshAgent>();
        _health = GetComponent<EnemyHealth>();
        _mpb    = new MaterialPropertyBlock();

        _health.OnDeath += OnDeath;

        BuildTree();
        ApplyStateColor();

        if (_waypoints != null && _waypoints.Length > 0)
            _agent.SetDestination(_waypoints[0].position);
    }

    private void BuildTree()
    {
        _tree = new BtSelector(
            new BtSequence(new BtCondition(IsChasing), new BtAction(ChasePlayer)),
            new BtSequence(new BtCondition(IsAlert),   new BtAction(InvestigateAlert)),
            new BtSequence(new BtCondition(IsPatrol),  new BtAction(PatrolWaypoints))
        );
    }

    private void Update()
    {
        if (_health.Health <= 0f) return;

        _stunTimer.Tick(Time.deltaTime);

        if (IsStunned)
        {
            _agent.isStopped = true;
            _wasStunned = true;
            return;
        }

        if (_wasStunned)
        {
            _agent.isStopped = false;
            _wasStunned = false;
        }

        _seesPlayer = CanSeePlayer();
        DetectPlayer();
        _tree.Tick();

        _attackCooldown.Tick(Time.deltaTime);
    }

    private void DetectPlayer()
    {
        bool detected = _seesPlayer || CanHearPlayer();

        if (detected)
        {
            _lastKnownPos = _playerTransform.position;
            if (_state != AiState.Chase)
                SetState(AiState.Chase);
            return;
        }

        if (_state == AiState.Chase)
        {
            _alertTimer = _data.AlertDuration;
            SetState(AiState.Alert);
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 eyePos   = transform.position + Vector3.up * 1.5f;
        Vector3 toPlayer = _playerTransform.position - eyePos;
        float   dist     = toPlayer.magnitude;

        if (dist > _data.SightRange) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > _data.SightAngle * 0.5f) return false;

        return !Physics.Raycast(eyePos, toPlayer.normalized, dist, _obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private bool CanHearPlayer()
        => Vector3.Distance(transform.position, _playerTransform.position) <= _data.HearingRadius;

    private bool IsPatrol()  => _state == AiState.Patrol;
    private bool IsAlert()   => _state == AiState.Alert;
    private bool IsChasing() => _state == AiState.Chase;

    private BtStatus PatrolWaypoints()
    {
        if (_waypoints == null || _waypoints.Length == 0) return BtStatus.Running;

        _agent.speed = _data.PatrolSpeed * _speedMultiplier;

        if (!_agent.pathPending && _agent.hasPath && _agent.remainingDistance < 0.4f)
        {
            _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
            _agent.SetDestination(_waypoints[_waypointIndex].position);
        }

        return BtStatus.Running;
    }

    private BtStatus InvestigateAlert()
    {
        _agent.speed = _data.PatrolSpeed * _speedMultiplier;
        _alertTimer -= Time.deltaTime;

        if (_alertTimer <= 0f || (!_agent.pathPending && _agent.hasPath && _agent.remainingDistance < 0.5f))
            SetState(AiState.Patrol);

        return BtStatus.Running;
    }

    private BtStatus ChasePlayer()
    {
        if (_data.CombatType == EnemyCombatType.Ranged)
            return ChasePlayerRanged();

        return ChasePlayerMelee();
    }

    private BtStatus ChasePlayerMelee()
    {
        _agent.speed = _data.ChaseSpeed * _speedMultiplier;

        float distSq = (transform.position - _playerTransform.position).sqrMagnitude;
        if (distSq <= _data.AttackRange * _data.AttackRange)
        {
            _agent.isStopped = true;
            TryMeleeAttack();
        }
        else
        {
            _agent.isStopped = false;
            _agent.SetDestination(_playerTransform.position);
        }

        return BtStatus.Running;
    }

    private BtStatus ChasePlayerRanged()
    {
        _agent.speed = _data.ChaseSpeed * _speedMultiplier;

        Vector3 toPlayer = _playerTransform.position - transform.position;
        float dist = toPlayer.magnitude;
        float preferred = _data.PreferredRange;
        float tolerance = preferred * 0.15f;

        if (_burstRemaining > 0)
        {
            _agent.isStopped = true;
            FacePlayer();
            TickBurst();
            return BtStatus.Running;
        }

        if (dist > preferred + tolerance)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_playerTransform.position);
        }
        else if (dist < preferred - tolerance)
        {
            _agent.isStopped = false;
            Vector3 retreatDir = -toPlayer.normalized;
            Vector3 retreatTarget = transform.position + retreatDir * (preferred - dist + tolerance);
            if (NavMesh.SamplePosition(retreatTarget, out var hit, tolerance * 2f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }
        else
        {
            Strafe();

            if (_seesPlayer)
                TryRangedAttack();
        }

        FacePlayer();
        return BtStatus.Running;
    }

    private void Strafe()
    {
        _strafeTimer -= Time.deltaTime;

        if (_strafeTimer <= 0f)
        {
            _strafeTimer = _data.StrafeInterval;
            _strafeDir = Random.value > 0.5f ? 1 : -1;
        }

        Vector3 toPlayer = (_playerTransform.position - transform.position).normalized;
        Vector3 strafeVec = Vector3.Cross(Vector3.up, toPlayer) * _strafeDir;
        Vector3 strafeTarget = transform.position + strafeVec * _data.StrafeDistance;

        if (NavMesh.SamplePosition(strafeTarget, out var hit, _data.StrafeDistance, NavMesh.AllAreas))
        {
            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
        }
        else
        {
            _strafeDir = -_strafeDir;
        }
    }

    private void FacePlayer()
    {
        Vector3 dir = _playerTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(dir),
                _agent.angularSpeed * Time.deltaTime
            );
    }

    private void TryMeleeAttack()
    {
        if (!_attackCooldown.IsReady) return;
        _attackCooldown.Start(_data.AttackCooldown);
        _playerStats.TakeDamage(new DamageInfo(_data.AttackDamage));
        _data.AttackSound?.Play(transform.position);
    }

    private void TryRangedAttack()
    {
        if (!_attackCooldown.IsReady) return;
        _attackCooldown.Start(_data.AttackCooldown);

        _burstRemaining = Mathf.Max(1, _data.BurstCount);
        _burstTimer = 0f;
        FireOneShot();
        _burstRemaining--;
    }

    private void TickBurst()
    {
        if (_burstRemaining <= 0) return;

        _burstTimer -= Time.deltaTime;
        if (_burstTimer <= 0f)
        {
            FireOneShot();
            _burstRemaining--;
            _burstTimer = _data.BurstInterval;
        }
    }

    private void FireOneShot()
    {
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 dir = (_playerTransform.position + Vector3.up * 1f - eyePos).normalized;

        if (_data.SpreadAngle > 0f)
        {
            float halfSpread = _data.SpreadAngle * 0.5f;
            Vector3 randomOffset = Random.insideUnitCircle * Mathf.Tan(halfSpread * Mathf.Deg2Rad);
            dir = (dir + transform.right * randomOffset.x + transform.up * randomOffset.y).normalized;
        }

        SoundBank sound = _data.RangedAttackSound != null ? _data.RangedAttackSound : _data.AttackSound;
        sound?.Play(transform.position);

        if (Physics.Raycast(eyePos, dir, out var hit, _data.SightRange, ~0, QueryTriggerInteraction.Ignore))
        {
            var target = hit.collider.GetComponentInParent<PlayerStats>();
            if (target != null)
            {
                bool headshot = hit.collider.CompareTag("Head");
                float damage = headshot ? _data.AttackDamage * 2f : _data.AttackDamage;
                target.TakeDamage(new DamageInfo(damage));
            }
        }
    }

    private void SetState(AiState state)
    {
        _agent.isStopped = false;
        _state = state;
        _burstRemaining = 0;

        switch (state)
        {
            case AiState.Alert:
                _agent.SetDestination(_lastKnownPos);
                break;
            case AiState.Patrol:
                if (_waypoints != null && _waypoints.Length > 0)
                    _agent.SetDestination(_waypoints[_waypointIndex].position);
                break;
        }

        ApplyStateColor();
    }

    private void ApplyStateColor()
    {
        Color color = _state switch
        {
            AiState.Alert  => _alertColor,
            AiState.Chase  => _chaseColor,
            _              => _patrolColor
        };

        _mpb.SetColor(ColorId, color);
        foreach (var r in _stateRenderers)
        {
            if (r != null) r.SetPropertyBlock(_mpb);
        }
    }

    private void OnDeath()
    {
        _data.DeathSound?.Play(transform.position);
        _agent.enabled = false;
        enabled = false;
    }
}
