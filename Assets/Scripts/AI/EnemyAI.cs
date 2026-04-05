using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
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
    private float   _attackCooldown;
    private bool    _seesPlayer;

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

        _seesPlayer = CanSeePlayer();
        DetectPlayer();
        _tree.Tick();

        if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;
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

        _agent.speed = _data.PatrolSpeed;

        if (!_agent.pathPending && _agent.hasPath && _agent.remainingDistance < 0.4f)
        {
            _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
            _agent.SetDestination(_waypoints[_waypointIndex].position);
        }

        return BtStatus.Running;
    }

    private BtStatus InvestigateAlert()
    {
        _agent.speed = _data.PatrolSpeed;
        _alertTimer -= Time.deltaTime;

        if (_alertTimer <= 0f || (!_agent.pathPending && _agent.hasPath && _agent.remainingDistance < 0.5f))
            SetState(AiState.Patrol);

        return BtStatus.Running;
    }

    private BtStatus ChasePlayer()
    {
        _agent.speed = _data.ChaseSpeed;

        float distSq = (transform.position - _playerTransform.position).sqrMagnitude;
        if (distSq <= _data.AttackRange * _data.AttackRange)
        {
            _agent.isStopped = true;
            TryAttack();
        }
        else
        {
            _agent.isStopped = false;
            _agent.SetDestination(_playerTransform.position);
        }

        return BtStatus.Running;
    }

    private void TryAttack()
    {
        if (_attackCooldown > 0f) return;
        _attackCooldown = _data.AttackCooldown;
        _playerStats.TakeDamage(_data.AttackDamage);
    }

    private void SetState(AiState state)
    {
        _agent.isStopped = false;
        _state = state;

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
        _agent.enabled = false;
        enabled = false;
    }
}
