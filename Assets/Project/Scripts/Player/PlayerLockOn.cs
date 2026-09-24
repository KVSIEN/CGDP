using UnityEngine;
using CGD.Combat;
using CGD.Input;
using CGD.Targeting;

namespace CGD.Player
{
    // Lock-on: pressing LockOn picks the target closest to the centre of view (from a
    // TargetSelector, e.g. a forward cone) and PlayerCamera keeps the aim on it. Press
    // again to release; the lock also drops when the target dies or gets too far away.
    public class PlayerLockOn : MonoBehaviour
    {
        [SerializeField] private PlayerCamera   _camera;
        [Tooltip("Which characters can be locked, e.g. Data/Targeting/DefaultConeTargetSelector")]
        [SerializeField] private TargetSelector _selector;
        [SerializeField, Min(1f)] private float _maxDistance = 30f;
        [Tooltip("Aim point above the target's pivot (chest height)")]
        [SerializeField] private float _aimHeight = 1.2f;

        private readonly TargetSet _candidates = new();
        private PlayerInputHandler _input;
        private HealthManager      _self;
        private HealthManager      _target;

        public HealthManager Target => _target;

        private void Awake()
        {
            TryGetComponent(out _input);
            TryGetComponent(out _self);
        }

        private void OnDisable() => Release();

        private void Update()
        {
            if (_input != null && _input.GetAction(GameAction.LockOn))
            {
                if (_target != null) Release();
                else                 Acquire();
            }

            if (_target != null && !IsStillValid(_target)) Release();
        }

        private void Acquire()
        {
            if (_selector == null || _camera == null) return;

            Transform view = _camera.transform;
            _selector.Select(new TargetingRequest(view.position, view.forward, _self), _candidates);

            HealthManager best = null;
            float bestAngle = float.MaxValue;
            foreach (HealthManager candidate in _candidates.Targets)
            {
                if (!IsStillValid(candidate)) continue;

                float angle = Vector3.Angle(view.forward, AimPoint(candidate) - view.position);
                if (angle >= bestAngle) continue;

                best      = candidate;
                bestAngle = angle;
            }

            if (best == null) return;

            _target = best;
            _camera.SetLockTarget(best.transform, _aimHeight);
        }

        private void Release()
        {
            if (_target == null) return;

            _target = null;
            if (_camera != null) _camera.ClearLockTarget();
        }

        private bool IsStillValid(HealthManager target) =>
            target != null && target.isActiveAndEnabled && !target.IsDead
            && (target.transform.position - transform.position).sqrMagnitude <= _maxDistance * _maxDistance;

        private Vector3 AimPoint(HealthManager target) => target.transform.position + Vector3.up * _aimHeight;
    }
}
