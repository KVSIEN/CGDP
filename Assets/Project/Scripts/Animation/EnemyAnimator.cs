using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Core;
using CGD.Enemies;

namespace CGD.Animation
{
    // Feeds an enemy's movement and AI state into its model's Animator (see AnimatorParams).
    // Purely visual — attacks still land on EnemyData's wind-up timing, so clips should be
    // authored to match it rather than the other way round.
    // Lives on the enemy root next to EnemyAI.
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyAnimator : MonoBehaviour, IPoolable
    {
        [Tooltip("Animator on the enemy's model")]
        [SerializeField] private Animator _animator;
        [Tooltip("Smoothing for the locomotion blend inputs, seconds")]
        [SerializeField] private float _locomotionDamping = 0.1f;

        private AnimatorBridge _bridge;
        private EnemyAI        _ai;
        private NavMeshAgent   _agent;
        private HealthManager  _health;

        private void Awake()
        {
            _bridge = new AnimatorBridge(_animator);
            _ai     = GetComponent<EnemyAI>();
            TryGetComponent(out _agent);
            TryGetComponent(out _health);
        }

        private void OnEnable()
        {
            _ai.AttackStarted += OnAttackStarted;
            _ai.Fired         += OnFired;
            if (_health != null) _health.OnHit += OnHit;
        }

        private void OnDisable()
        {
            _ai.AttackStarted -= OnAttackStarted;
            _ai.Fired         -= OnFired;
            if (_health != null) _health.OnHit -= OnHit;
        }

        private void Update()
        {
            if (!_bridge.IsActive) return;

            float dt = Time.deltaTime;
            Vector3 velocity = _agent != null && _agent.enabled ? _agent.velocity : Vector3.zero;
            Vector3 local    = _animator.transform.InverseTransformDirection(new Vector3(velocity.x, 0f, velocity.z));

            _bridge.SetFloat(AnimatorParams.Speed,        local.magnitude, _locomotionDamping, dt);
            _bridge.SetFloat(AnimatorParams.ForwardSpeed, local.z,         _locomotionDamping, dt);
            _bridge.SetFloat(AnimatorParams.StrafeSpeed,  local.x,         _locomotionDamping, dt);
            _bridge.SetBool(AnimatorParams.Grounded, true);

            var state = _ai.State;
            _bridge.SetBool(AnimatorParams.Alerted, state == EnemyAI.AiState.Alert || state == EnemyAI.AiState.Chase);
            _bridge.SetBool(AnimatorParams.Stunned, state == EnemyAI.AiState.Stunned);
            _bridge.SetBool(AnimatorParams.Dead,    _health != null && _health.IsDead);
        }

        private void OnAttackStarted() => _bridge.SetTrigger(AnimatorParams.Attack);

        private void OnFired() => _bridge.SetTrigger(AnimatorParams.Fire);

        private void OnHit(DamageInfo _) => _bridge.SetTrigger(AnimatorParams.Hit);

        // Back from the pool: start from the entry state, not the end of the death clip.
        public void OnSpawned() => _bridge.Rebind();
    }
}
