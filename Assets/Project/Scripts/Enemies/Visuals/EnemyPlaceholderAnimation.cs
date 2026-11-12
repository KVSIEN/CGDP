using UnityEngine;

namespace CGD.Enemies
{
    // Stand-in for animation on enemies without a rig (the capsule test agents), so what they
    // do reads at a glance: an arm that pulls back through a melee wind-up and thrusts out as
    // the hit lands, held up while an ability is being cast; the whole model toppling over
    // on death, standing straight back up when the enemy is revived; and growing back in
    // from nothing where it reappears after a Phase Shift.
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyPlaceholderAnimation : MonoBehaviour
    {
        private const float StrikeTime  = 0.08f;
        private const float RecoverTime = 0.2f;
        private const float FallTime    = 0.35f;
        private const float CastPulse   = 10f;   // radians per second the casting arm pulses at
        private const float ReappearTime = 0.35f;

        private static readonly Quaternion Fallen = Quaternion.Euler(90f, 0f, 0f);

        [Tooltip("Pivot at the feet holding the visible model; tipped over on death")]
        [SerializeField] private Transform _model;
        [Tooltip("Shown only while attacking or casting")]
        [SerializeField] private Transform _arm;
        [Tooltip("Metres the arm draws back during the wind-up")]
        [SerializeField, Min(0f)] private float _pullBack = 0.3f;
        [Tooltip("Metres the arm thrusts forward on the strike")]
        [SerializeField, Min(0f)] private float _reach = 0.8f;
        [Tooltip("Metres the arm is raised while casting")]
        [SerializeField] private float _castLift = 0.35f;
        [Tooltip("How high the fallen model rests (its body's radius), so it lies on the floor")]
        [SerializeField, Min(0f)] private float _lyingHeight = 0.45f;

        private EnemyAI     _ai;
        private EnemyHealth _health;
        private Vector3     _armRest;
        private Vector3     _armScale;
        private Vector3     _modelRest;
        private Vector3     _modelScale = Vector3.one;
        private float       _reappearClock = -1f;   // seconds since it reappeared; < 0 = not reappearing
        private float       _attackClock = -1f;   // seconds since the melee attack began; < 0 = none
        private float       _fallClock   = -1f;   // seconds since death; < 0 = standing

        private void Awake()
        {
            _ai     = GetComponent<EnemyAI>();
            _health = GetComponent<EnemyHealth>();
            if (_model != null)
            {
                _modelRest  = _model.localPosition;
                _modelScale = _model.localScale;
            }
            if (_arm == null) return;

            _armRest  = _arm.localPosition;
            _armScale = _arm.localScale;
            _arm.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _ai.AttackStarted += OnAttackStarted;
            _ai.Relocated     += OnRelocated;
            _health.OnDeath   += OnDeath;
            _health.OnRevived += StandUp;
        }

        private void OnDisable()
        {
            _ai.AttackStarted -= OnAttackStarted;
            _ai.Relocated     -= OnRelocated;
            _health.OnDeath   -= OnDeath;
            _health.OnRevived -= StandUp;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_reappearClock >= 0f) TickReappear(dt);
            if (_fallClock >= 0f) TickFall(dt);
            else if (_arm != null) TickArm(dt);
        }

        private void OnAttackStarted() => _attackClock = 0f;

        private void OnRelocated(Vector3 from) => _reappearClock = 0f;

        private void TickReappear(float dt)
        {
            if (_model == null) return;
            _reappearClock += dt;
            float t = Mathf.Clamp01(_reappearClock / ReappearTime);
            _model.localScale = _modelScale * Mathf.Max(0.05f, t * (2f - t));   // eases out; never a zero-size hitbox
            if (t >= 1f) _reappearClock = -1f;
        }

        private void OnDeath()
        {
            _attackClock = -1f;
            _fallClock   = 0f;
            if (_arm != null) _arm.gameObject.SetActive(false);
        }

        private void StandUp()
        {
            _fallClock     = -1f;
            _reappearClock = -1f;
            if (_model == null) return;
            _model.localScale    = _modelScale;
            _model.localPosition = _modelRest;
            _model.localRotation = Quaternion.identity;
        }

        private void TickFall(float dt)
        {
            if (_model == null) return;
            _fallClock += dt;
            float t = Mathf.Clamp01(_fallClock / FallTime);
            float eased = t * t;   // accelerates like something tipping over
            _model.localRotation = Quaternion.Slerp(Quaternion.identity, Fallen, eased);
            _model.localPosition = _modelRest + Vector3.up * (_lyingHeight * eased);
        }

        private void TickArm(float dt)
        {
            if (_ai.Abilities != null && _ai.Abilities.IsCasting)
            {
                float pulse = 1f + 0.25f * Mathf.Sin(Time.time * CastPulse);
                ShowArm(_armRest + Vector3.up * _castLift, _armScale * pulse);
                return;
            }

            if (_attackClock < 0f)
            {
                _arm.gameObject.SetActive(false);
                return;
            }

            _attackClock += dt;
            float windup = _ai.Data.AttackWindup;

            // A stagger cancels the wind-up: the swing never comes.
            if (_attackClock < windup && !_ai.IsAttacking) _attackClock = float.MaxValue;

            float forward;
            if (_attackClock < windup)
                forward = -_pullBack * (_attackClock / windup);
            else if (_attackClock < windup + StrikeTime)
                forward = Mathf.Lerp(-_pullBack, _reach, (_attackClock - windup) / StrikeTime);
            else if (_attackClock < windup + StrikeTime + RecoverTime)
                forward = Mathf.Lerp(_reach, 0f, (_attackClock - windup - StrikeTime) / RecoverTime);
            else
            {
                _attackClock = -1f;
                _arm.gameObject.SetActive(false);
                return;
            }

            ShowArm(_armRest + Vector3.forward * forward, _armScale);
        }

        private void ShowArm(Vector3 localPosition, Vector3 localScale)
        {
            _arm.gameObject.SetActive(true);
            _arm.localPosition = localPosition;
            _arm.localScale    = localScale;
        }
    }
}
