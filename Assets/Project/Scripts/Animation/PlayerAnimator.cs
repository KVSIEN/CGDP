using UnityEngine;
using CGD.Combat;
using CGD.Player;
using CGD.Weapons;

namespace CGD.Animation
{
    // Feeds the player's movement and combat state into the body model's Animator (see
    // AnimatorParams for the parameter names). Purely visual: gameplay never waits on an
    // animation, so the player plays the same with or without a rigged body.
    // Lives on the Player next to the components it reads.
    public class PlayerAnimator : MonoBehaviour
    {
        [Tooltip("Animator on the body model (Player Body)")]
        [SerializeField] private Animator _animator;
        [Tooltip("Optional — drives the Aim parameter")]
        [SerializeField] private PlayerCamera _camera;
        [Tooltip("Smoothing for the locomotion blend inputs, seconds")]
        [SerializeField] private float _locomotionDamping = 0.1f;

        private AnimatorBridge    _bridge;
        private PlayerMovement    _movement;
        private PlayerDodge       _dodge;
        private HealthManager     _health;
        private WeaponController  _weapons;
        private MeleeController   _melee;
        private GrenadeController _grenades;

        private void Awake()
        {
            _bridge = new AnimatorBridge(_animator);
            TryGetComponent(out _movement);
            TryGetComponent(out _dodge);
            TryGetComponent(out _health);
            TryGetComponent(out _weapons);
            TryGetComponent(out _melee);
            TryGetComponent(out _grenades);
        }

        private void OnEnable()
        {
            if (_health   != null) { _health.OnHit += OnHit; _health.OnRevived += OnRevived; }
            if (_weapons  != null) _weapons.Fired += OnFired;
            if (_melee    != null) _melee.AttackStarted += OnAttackStarted;
            if (_grenades != null) _grenades.Thrown += OnThrown;
        }

        private void OnDisable()
        {
            if (_health   != null) { _health.OnHit -= OnHit; _health.OnRevived -= OnRevived; }
            if (_weapons  != null) _weapons.Fired -= OnFired;
            if (_melee    != null) _melee.AttackStarted -= OnAttackStarted;
            if (_grenades != null) _grenades.Thrown -= OnThrown;
        }

        private void Update()
        {
            if (!_bridge.IsActive) return;

            float dt = Time.deltaTime;
            if (_movement != null) UpdateLocomotion(dt);

            _bridge.SetBool(AnimatorParams.Rolling,   _dodge != null && _dodge.IsRolling);
            _bridge.SetBool(AnimatorParams.Dashing,   _dodge != null && _dodge.IsDashing);
            _bridge.SetBool(AnimatorParams.Reloading, _weapons != null && _weapons.IsReloading);
            _bridge.SetBool(AnimatorParams.Dead,      _health != null && _health.IsDead);
            if (_camera != null) _bridge.SetFloat(AnimatorParams.Aim, _camera.AdsT);
        }

        private void UpdateLocomotion(float dt)
        {
            Vector3 velocity = _movement.Velocity;
            Vector3 local    = _animator.transform.InverseTransformDirection(new Vector3(velocity.x, 0f, velocity.z));

            _bridge.SetFloat(AnimatorParams.Speed,        local.magnitude, _locomotionDamping, dt);
            _bridge.SetFloat(AnimatorParams.ForwardSpeed, local.z,         _locomotionDamping, dt);
            _bridge.SetFloat(AnimatorParams.StrafeSpeed,  local.x,         _locomotionDamping, dt);
            _bridge.SetFloat(AnimatorParams.VerticalSpeed, velocity.y);

            _bridge.SetBool(AnimatorParams.Grounded,  _movement.IsGrounded);
            _bridge.SetBool(AnimatorParams.Crouching, _movement.IsCrouching);
            _bridge.SetBool(AnimatorParams.Sprinting, _movement.IsSprinting);
            _bridge.SetBool(AnimatorParams.Sliding,   _movement.IsSliding);
            _bridge.SetBool(AnimatorParams.Mantling,  _movement.IsMantling);
            _bridge.SetBool(AnimatorParams.Stunned,   _movement.IsStunned);
        }

        private void OnHit(DamageInfo _) => _bridge.SetTrigger(AnimatorParams.Hit);

        private void OnFired() => _bridge.SetTrigger(AnimatorParams.Fire);

        private void OnThrown() => _bridge.SetTrigger(AnimatorParams.Throw);

        private void OnAttackStarted(int comboIndex)
        {
            _bridge.SetInteger(AnimatorParams.AttackIndex, comboIndex);
            _bridge.SetTrigger(AnimatorParams.Attack);
        }

        // Respawn starts from the entry state instead of the end of the death clip.
        private void OnRevived() => _bridge.Rebind();
    }
}
